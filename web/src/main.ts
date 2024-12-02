import LoadingSpinner from "@components/LoadingSpinner.vue"
import "@styles/index.scss"
import { BootstrapVue, BootstrapVueIcons } from "bootstrap-vue"
import "intersection-observer"
import Vue from "vue"
import VueResource from "vue-resource"
import VueRouter from "vue-router"
import App from "./App.vue"
import "./filters"
import ServicePlugin from "./plugins/ServicePlugin"
import routes from "./router/index"
import store from "./store/index"

Vue.use(VueResource)
Vue.use(VueRouter)
Vue.use(BootstrapVue)
Vue.use(BootstrapVueIcons)
Vue.use(ServicePlugin)
Vue.config.productionTip = true
Vue.component("loading-spinner", LoadingSpinner)

Vue.http.interceptors.push(function() {
  return function(response) {
    if (response.status == 401) {
      location.replace(`${import.meta.env.BASE_URL}api/auth/login?redirectUri=${window.location}`)
    }
  }
})

Vue.http.options.root = import.meta.env.BASE_URL

// Redirect from / to /jasper/
if (location.pathname == "/")
  history.pushState({ page: "home" }, "", import.meta.env.BASE_URL)

const router = new VueRouter({
  mode: "history",
  base: import.meta.env.BASE_URL,
  routes: routes
})

new Vue({
  router,
  store,
  render: (h) => h(App)
}).$mount("#app")
