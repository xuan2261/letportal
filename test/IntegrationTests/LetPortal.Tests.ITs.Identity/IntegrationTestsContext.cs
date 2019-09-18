using LetPortal.Core.Persistences;
using LetPortal.Core.Utils;
using LetPortal.Identity.Entities;
using LetPortal.Identity.Repositories.Identity;
using LetPortal.Identity.Stores;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LetPortal.Tests.ITs.Identity
{
    public class IntegrationTestsContext : IDisposable
    {
        public DatabaseOptions MongoDatabaseOptions { get; }

        public static bool isRegistered;
        private static readonly object _lockObject = new object();

        public IntegrationTestsContext()
        {
            bool lockTaken = false;
            Monitor.Enter(_lockObject, ref lockTaken);
            try
            {
                if(!isRegistered)
                {
                    ConventionPackDefault.Register();
                    isRegistered = true;
                }
            }
            finally
            {
                if(lockTaken)
                {
                    Monitor.Exit(_lockObject);
                }
            }

            MongoDatabaseOptions = new DatabaseOptions
            {
                ConnectionString = "mongodb://localhost:27017",
                ConnectionType = ConnectionType.MongoDB,
                Datasource = generateUniqueDatasourceName()
            };

            createSomeDummyData();
        }

        public void Dispose()
        {
            // Remove all created databases
            var mongoClient = new MongoClient(MongoDatabaseOptions.ConnectionString);
            mongoClient.DropDatabase(MongoDatabaseOptions.Datasource);
        }

        public RoleStore GetRoleStore()
        {
            var roleStore = new RoleStore(getRoleMongoRepository());

            return roleStore;
        }

        public UserStore GetUserStore()
        {
            var userStore = new UserStore(getUserMongoRepository(), getRoleMongoRepository());
            return userStore;
        }

        public User GenerateUser()
        {
            var username = generateUniqueUserName();
            return new User
            {
                Id = DataUtil.GenerateUniqueId(),
                Username = username,
                NormalizedUserName = username.ToUpper(),
                Domain = string.Empty,
                PasswordHash = "AQAAAAEAACcQAAAAEBhhMYTL5kwYqXheHSdarA/+vleSI07yGkTKNw1bb1jrTlYnBZK1CZ+zdHnqWwLLDA==",
                Email = username + "@portal.com",
                NormalizedEmail = username.ToUpper() + "@PORTAL.COM",
                IsConfirmedEmail = true,
                SecurityStamp = "7YHYVBYWLTYC4EAPVRS2SWX2IIUOZ3XM",
                AccessFailedCount = 0,
                IsLockoutEnabled = false,
                LockoutEndDate = DateTime.UtcNow,
                Roles = new List<string>
                {
                    "SuperAdmin"
                },
                Claims = new List<BaseClaim>
                {
                    StandardClaims.AccessAppSelectorPage,
                    StandardClaims.Sub("5ce287ee569d6f23e8504cef"),
                    StandardClaims.UserId("5ce287ee569d6f23e8504cef"),
                    StandardClaims.Name(username)
                }
            };
        }

        public Role GenerateRole()
        {
            var roleName = generateUniqueRoleName();
            return new Role
            {
                Id = DataUtil.GenerateUniqueId(),
                Name = roleName,
                NormalizedName = roleName.ToUpper(),
                DisplayName = roleName,
                Claims = new List<BaseClaim>
                {
                    StandardClaims.AccessCoreApp("5c162e9005924c1c741bfdc2")
                }
            };
        }

        private string generateUniqueRoleName()
        {
            var suppliedVars = "abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            var lengthOfName = 20;
            var role = string.Empty;
            for(int i = 0; i < lengthOfName; i++)
            {
                var randomIndx = (new Random()).Next(0, 45);
                role += suppliedVars[randomIndx];
            }

            return role;
        }

        private string generateUniqueUserName()
        {
            var suppliedVars = "abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            var lengthOfName = 20;
            var username = string.Empty;
            for(int i = 0; i < lengthOfName; i++)
            {
                var randomIndx = (new Random()).Next(0, 45);
                username += suppliedVars[randomIndx];
            }

            return username;
        }

        private UserMongoRepository getUserMongoRepository()
        {
            var databaseOptions = MongoDatabaseOptions;
            var databaseOptionsMock = Mock.Of<IOptionsMonitor<DatabaseOptions>>(_ => _.CurrentValue == databaseOptions);
            var userMongoRepository = new UserMongoRepository(new MongoConnection(databaseOptionsMock.CurrentValue));
            return userMongoRepository;
        }

        private RoleMongoRepository getRoleMongoRepository()
        {
            var databaseOptions = MongoDatabaseOptions;
            var databaseOptionsMock = Mock.Of<IOptionsMonitor<DatabaseOptions>>(_ => _.CurrentValue == databaseOptions);
            var roleMongoRepository = new RoleMongoRepository(new MongoConnection(databaseOptionsMock.CurrentValue));
            return roleMongoRepository;
        }

        private string generateUniqueDatasourceName()
        {
            var suppliedVars = "abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            var lengthOfName = 20;
            var datasourceName = string.Empty;
            for(int i = 0; i < lengthOfName; i++)
            {
                var randomIndx = (new Random()).Next(0, 45);
                datasourceName += suppliedVars[randomIndx];
            }

            return datasourceName;
        }

        private void createSomeDummyData()
        {
            var mongoClient = new MongoClient(MongoDatabaseOptions.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(MongoDatabaseOptions.Datasource);
            var roleCollection = mongoDatabase.GetCollection<Role>("roles");
            var userCollection = mongoDatabase.GetCollection<User>("users");
            var superAdminRole = new Role
            {
                Id = "5c06a15e4cc9a850bca44488",
                Name = "SuperAdmin",
                NormalizedName = "SUPERADMIN",
                DisplayName = "Super Admin",
                Claims = new List<BaseClaim>
                {
                    StandardClaims.AccessCoreApp("5c162e9005924c1c741bfdc2")
                }
            };

            // Pass: @Dm1n!
            var adminAccount = new User
            {
                Id = "5ce287ee569d6f23e8504cef",
                Username = "admin",
                NormalizedUserName = "ADMIN",
                Domain = string.Empty,
                PasswordHash = "AQAAAAEAACcQAAAAEBhhMYTL5kwYqXheHSdarA/+vleSI07yGkTKNw1bb1jrTlYnBZK1CZ+zdHnqWwLLDA==",
                Email = "admin@portal.com",
                NormalizedEmail = "ADMIN@PORTAL.COM",
                IsConfirmedEmail = true,
                SecurityStamp = "7YHYVBYWLTYC4EAPVRS2SWX2IIUOZ3XM",
                AccessFailedCount = 0,
                IsLockoutEnabled = false,
                LockoutEndDate = DateTime.UtcNow,
                Roles = new List<string>
                {
                    "SuperAdmin"
                },
                Claims = new List<BaseClaim>
                {
                    StandardClaims.AccessAppSelectorPage,
                    StandardClaims.Sub("5ce287ee569d6f23e8504cef"),
                    StandardClaims.UserId("5ce287ee569d6f23e8504cef"),
                    StandardClaims.Name("admin")
                }
            };
            roleCollection.InsertOne(superAdminRole);
            userCollection.InsertOne(adminAccount);
        }
    }
}
