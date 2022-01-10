import Vue from 'vue'

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
