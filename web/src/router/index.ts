import { RouteConfig } from 'vue-router'
import Home from '@components/Home.vue'
import CivilCaseDetails from "@components/civil/CivilCaseDetails.vue";
import CriminalCaseDetails from "@components/criminal/CriminalCaseDetails.vue";
import CivilFileSearchResultsView from "@components/civil/CivilFileSearchResultsView.vue";
import CriminalFileSearchResultsView from "@components/criminal/CriminalFileSearchResultsView.vue";
import CourtList from "@components/courtlist/CourtList.vue";
import { SessionManager } from "@/utils/utils";
import store from "@/store/index";

async function authGuard(to: any, from: any, next: any) {
  const results = await SessionManager.getSettings(store);
  if (results){
    next();
  }  
}

const routes: Array<RouteConfig> = [
  {
    path: '/',
    name: 'Home',
    component: Home
  },
  {
    path: '/court-list',
    name: 'CourtList',
    component: CourtList,
    beforeEnter: authGuard, 
    props: true,
    children: [
      {
        path: 'location/:location/room/:room/date/:date',
        name: 'CourtListResult',
        component: CourtList, 
        props: true
      }
    ]
  },
  {
    path: '/civil-file/:fileNumber/:section?',
    name: 'CivilCaseDetails',
    component: CivilCaseDetails,
    beforeEnter: authGuard, 
    props: true
  },
  {
    path: '/criminal-file/:fileNumber',
    name: 'CriminalCaseDetails',
    component: CriminalCaseDetails,
    beforeEnter: authGuard,
    props: true
  },
  {
    path: '/civil-file-search',
    name: 'CivilFileSearchResultsView',
    component: CivilFileSearchResultsView, 
    beforeEnter: authGuard,
    props: true
  },
  {
    path: '/criminal-file-search',
    name: 'CriminalFileSearchResultsView',
    component: CriminalFileSearchResultsView,
    beforeEnter: authGuard,
    props: true
  }
]

export default routes