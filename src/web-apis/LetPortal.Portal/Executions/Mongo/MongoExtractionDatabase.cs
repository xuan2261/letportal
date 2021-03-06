﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LetPortal.Core.Persistences;
using LetPortal.Portal.Entities.Databases;
using LetPortal.Portal.Models.Databases;
using LetPortal.Portal.Models.Shared;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace LetPortal.Portal.Executions.Mongo
{
    public class MongoExtractionDatabase : IExtractionDatabase
    {
        public ConnectionType ConnectionType => ConnectionType.MongoDB;

        public async Task<ExtractingSchemaQueryModel> Extract(DatabaseConnection database, string formattedString, IEnumerable<ExecuteParamModel> parameters)
        {
            // queryJsonString sample
            // { "$query" : { 
            //      "users": [
            //          "$match" : { 'username': 'A' }
            //      ]
            //  }}
            // Note: we just support aggreation framework only
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (param.RemoveQuotes)
                    {
                        formattedString = formattedString.Replace("\"{{" + param.Name + "}}\"", param.ReplaceValue);
                    }
                    else
                    {
                        formattedString = formattedString.Replace("{{" + param.Name + "}}", param.ReplaceValue);
                    }

                }
            }
            var result = new ExtractingSchemaQueryModel();

            var parsingObject = JObject.Parse(formattedString);

            var mongoClient = new MongoClient(database.ConnectionString).GetDatabase(database.DataSource);

            var executionGroupTypes = parsingObject.Children().Select(a => (a as JProperty).Name);

            foreach (var executionGroupType in executionGroupTypes)
            {
                switch (executionGroupType)
                {
                    case Constants.QUERY_KEY:
                        var collectionNames = parsingObject[Constants.QUERY_KEY].Children().Select(a => (a as JProperty).Name);
                        foreach (var collectionName in collectionNames)
                        {
                            var mongoCollection = mongoClient.GetCollection<BsonDocument>(collectionName);
                            var collectionQuery = parsingObject[Constants.QUERY_KEY][collectionName].ToString(Newtonsoft.Json.Formatting.Indented);
                            var aggregatePipes = BsonSerializer.Deserialize<BsonDocument[]>(collectionQuery).Select(a => (PipelineStageDefinition<BsonDocument, BsonDocument>)a).ToList();

                            var aggregateFluent = mongoCollection.Aggregate();
                            foreach (var pipe in aggregatePipes)
                            {
                                aggregateFluent = aggregateFluent.AppendStage(pipe);
                            }

                            using (var executingCursor = await aggregateFluent.ToCursorAsync())
                            {
                                while (executingCursor.MoveNext())
                                {
                                    // Important note: this query must have one row result for extracting params and filters
                                    var currentDocument = executingCursor.Current.First();

                                    foreach (var element in currentDocument.Elements)
                                    {
                                        var columnField = new ColumnField
                                        {
                                            Name = element.Name,
                                            DisplayName = element.Name,
                                            FieldType = GetTypeByBsonDocument(element.Value)
                                        };

                                        result.ColumnFields.Add(columnField);
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        private string GetTypeByBsonDocument(BsonValue bsonValue)
        {
            if (bsonValue.IsBoolean)
            {
                return "boolean";
            }
            if (bsonValue.IsInt32
                || bsonValue.IsInt64
                || bsonValue.IsNumeric
                || bsonValue.IsDecimal128
                || bsonValue.IsDouble)
            {
                return "number";
            }
            if (bsonValue.IsValidDateTime)
            {
                return "datetime";
            }
            if (bsonValue.IsBsonArray)
            {
                return "list";
            }
            if (bsonValue.IsBsonDocument)
            {
                return "document";
            }
            return "string";
        }
    }
}
