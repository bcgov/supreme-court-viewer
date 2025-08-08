<template>
  <div>
    <b-card v-if="isMounted" no-body>
      <div>
        <b-row>
          <h3
            class="ml-5 my-1 p-0 font-weight-normal"
            v-if="!showSections['Orders']"
          >
            Orders ({{ NumberOfOrders }})
          </h3>
        </b-row>
        <hr class="mx-3 bg-light" style="height: 5px;" />
      </div>

      <b-card v-if="!isDataReady && isMounted">
        <span class="text-muted ml-4 mb-5"> No Orders. </span>
      </b-card>

      <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
        <b-overlay :show="true">
          <b-card style="min-height: 100px;" />
          <template v-slot:overlay>
            <div>
              <loading-spinner />
              <p id="loading-label">Loading ...</p>
            </div>
          </template>
        </b-overlay>
      </b-card>

      <b-card
        bg-variant="light"
        v-if="isDataReady"
        style="max-height: 500px; overflow-y: auto;"
        no-body
        class="mx-3 mb-5"
      >
        <b-table
          :items="orders"
          :fields="fields"
          sort-by="appearanceDate"
          :sort-desc.sync="sortDesc"
          :no-sort-reset="true"
          sort-icon-left
          small
          striped
          responsive="sm"
        >
          <template
            v-for="(field, index) in fields"
            v-slot:[`head(${field.key})`]="data"
          >
            <b v-bind:key="index" :class="field.headerStyle">
              {{ data.label }}</b
            >
          </template>

          <template v-slot:cell(partyName)="data">
            <div v-for="(partyName, index) in data.value" v-bind:key="index">
              <span :style="data.field.cellStyle"> - {{ partyName }}</span>
            </div>
          </template>

          <template v-slot:cell(virtualChamberLink)="data">
            <b-button
              variant="outline-primary text-info"
              :style="data.field.cellStyle"
              @click="openOrder(data)"
              size="sm"
            >
              Order
            </b-button>
          </template>

          <template v-slot:cell(appearanceDate)="data">
            <span :style="data.field.cellStyle">
              {{ data.value | beautify_date }}
            </span>
          </template>

          <template v-slot:cell(descriptionText)="data">
            <div
              :style="data.field.cellStyle"
              v-b-tooltip.hover
              :title="data.value"
            >
              {{ data.value }}
            </div>
          </template>
        </b-table>
      </b-card>
    </b-card>
  </div>
</template>

<script lang="ts">
import { civilFileInformationType } from "@/types/civil";
import "@store/modules/CivilFileInformation";
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-class";
import CustomOverlay from "../CustomOverlay.vue";
const civilState = namespace("CivilFileInformation");

@Component({
  components: {
    CustomOverlay,
  },
})
export default class CivilOrdersView extends Vue {
  @civilState.State
  public showSections;

  @civilState.State
  public civilFileInformation!: civilFileInformationType;

  isMounted = false;
  isDataReady = false;
  sortDesc = false;
  fields: any = [];
  orders: any[] = [];
  partyNames: string[] = [];

  initialFields = [
    {
      key: "select",
      label: "",
      sortable: false,
      headerStyle: "text-primary",
      cellStyle: "font-size: 16px;",
      tdClass: "border-top",
      thClass: "",
    },
    {
      key: "partyName",
      label: "Party Names",
      sortable: true,
      headerStyle: "text-primary",
      cellStyle: "font-size: 14px;",
    },

    {
      key: "virtualChamberLink",
      label: "Orders",
      sortable: false,
      headerStyle: "text-primary",
      cellStyle: "border:0px; font-size: 16px;text-align:left;",
    },
    {
      key: "appearanceDate",
      label: "Appearance Date",
      sortable: true,
      headerStyle: "text",
      cellStyle: "font-size: 14px;",
    },
    {
      key: "descriptionText",
      label: "Description",
      sortable: false,
      headerStyle: "text",
      cellStyle: "font-size: 14px;",
    },
  ];

  public getOrders(): void {
    this.partyNames = this.civilFileInformation.detailsData.party.map(
      (p) => p.fullName
    );
    this.civilFileInformation.detailsData.civilCourtList.virtualChamberLink.forEach(
      (link) => {
        const match = link.match(/packageId=(\d+)/);
        this.orders.push({
          appearanceDate: this.civilFileInformation.detailsData.civilCourtList
            .appearanceDate,
          virtualChamberLink: link,
          partyName: this.partyNames,
          descriptionText: `Package ID: ${match ? match[1] : ""}`,
        });
      }
    );
    this.fields = JSON.parse(JSON.stringify(this.initialFields));
    if (this.orders.length > 0) {
      this.isDataReady = true;
    }
    this.isMounted = true;
  }

  mounted() {
    this.getOrders();
  }

  public openOrder(data) {
    const url = data.item.virtualChamberLink;
    if (url) {
      window.open(url, "_blank");
    }
  }

  get NumberOfOrders() {
    return this.orders.length;
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
