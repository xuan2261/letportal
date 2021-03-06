import { Injectable } from '@angular/core';
import { ObjectUtils } from '../utils/object-util';
import { ShellConfig, ShellConfigType } from '../shell/shell.model';

@Injectable({
    providedIn: 'root'
})
export class ConfigurationProvider {
    private configuration = {
        portalBaseEndpoint:'',
        identityBaseEndpoint: ''
    }
    constructor() {
        this.configuration = { ...JSON.parse(window.sessionStorage.getItem('configurations')) };
    }

    getCurrentConfigs(){
        return this.configuration
    }

    getShellConfigs(): Array<ShellConfig> {
        const flatten: any = ObjectUtils.flattenObjects({ configs: this.configuration })

        let shellConfigs: Array<ShellConfig> = []
        for (var prop in flatten) {
            shellConfigs.push({
                key: prop,
                value: flatten[prop],
                type: ShellConfigType.Constant
            })
        }

        return shellConfigs
    }    
}