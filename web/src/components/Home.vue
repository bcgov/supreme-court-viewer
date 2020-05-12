<template>
    <div class="home">
        <br />
        <div class="mb-2">
            <h2> Temporary landing page for SCV </h2>
        </div>
        <div>
            <!-- <b-col class="mb-2" sm="9">
                <label for="proceeding-datepicker">Proceeding date:</label>
                <b-form-datepicker id="proceeding-datepicker" v-model="civilFileDocument.proceedingDate" class="mb-2"></b-form-datepicker>
            </b-col>
            <b-col class="mb-2" sm="9">
                <label for="location"> Location: </label>
                <b-form-input id="location" v-model="civilFileDocument.location" placeholder="Enter location"></b-form-input>
            </b-col>
            <b-col class="mb-2" sm="9">
                <label for="room"> Room: </label>
                <b-form-input id="room" v-model="civilFileDocument.room" placeholder="Enter room"></b-form-input>
            </b-col> -->
            <b-col class="mb-3" sm="9">
                <label for="filenumber"> File number: </label>
                <b-form-input id="filenumber" v-model="civilFileDocument.fileNumber" placeholder="Enter file number" @keyup.enter="navigateToDocumentsView(civilFileDocument)"></b-form-input>
            </b-col>
            <b-col class="mb-3" sm="9">
                <b-button @click="navigateToDocumentsView(civilFileDocument)">Search</b-button>
            </b-col>
        </div>
    </div>
</template>

<script lang="ts">
    import { Component, Vue } from 'vue-property-decorator';
    import { namespace } from 'vuex-class'
    import CivilFileDocuments from '../store/modules/CivilFileDocuments';
    const civilState = namespace('CivilFileDocuments');

    @Component
    export default class Home extends Vue {
        
        @civilState.State
        public civilFileDocument!: any

        @civilState.Action
        public UpdateCivilFile!: (newCivilFileDocument: any) => void
        // TODO: add validation so that the user has to enter values before clicking the search button
        navigateToDocumentsView(civilFileDocument): void {
            this.UpdateCivilFile(civilFileDocument)
            this.$router.push('/civil-documents')
        }        
    }
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
    button {
        background-color: #036
    }

    input {
        width: 300px;
    }

    #proceeding-datepicker__outer_ {
        width: 300px;
    }

    .home {
        margin-left: 25px;
    }

    .fade-enter-active, .fade-leave-active {
        transition: opacity .5s;
    }

    .fade-enter, .fade-leave-to /* .fade-leave-active below version 2.1.8 */ {
        opacity: 0;
    }
</style>
