import Vue from 'vue'
import { Logger as SplunkLogger } from 'splunk-logging';

export const SessionManager = {
    getSettings: async function(store) {
        try {
            if (!store.state.userInfo) {
                const response = await Vue.http.get('api/auth/info');
                store.commit("CommonInformation/setEnableArchive", response.body.enableArchive);
                store.commit("CommonInformation/setUserInfo", response.body);
            }
            return true;
        }
        catch (error) {
            console.log(error);  
            return false;
        }
    }
}

export const splunkLog = (message) => {
    debugger;
    const config = {
        token: process.env["SPLUNK_TOKEN"],
        url: process.env["SPLUNK_COLLECTOR_URL"]
    };
  
    const Logger = new SplunkLogger(config);
  
    const payload = {
        message: message
    };
  
    //console.log("Sending payload", payload);
    Logger.send(payload, (err, resp, body)=> {
        console.log("Response from Splunk", body);
    });
  };
