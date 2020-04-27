import Vue from 'vue';
import App from './App.vue';
import "./styles/index.scss";
import "bootstrap-vue/dist/bootstrap-vue.css";

Vue.config.productionTip = true;

new Vue({
    render: h => h(App)
}).$mount('#app');