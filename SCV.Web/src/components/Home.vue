
<template>
    <div class="home">
        <br />
        <transition name="fade" mode="out-in">
            <div :key="msg"> 
                <h1 key="msg">{{ msg }}</h1>
            </div>
        </transition>
    </div>
</template>

<script lang="ts">
    import axios from 'axios';
    import { Component, Prop, Vue } from 'vue-property-decorator';

    @Component
    export default class Home extends Vue {
        msg :string = 'Fetching data from api...';

        fetchApi(): void {
            debugger;
            axios.defaults.baseURL = `https://localhost:${process.env.VUE_APP_API_PORT}`;
            let component = this;
            axios.get('data')
                .then(function (response) {
                    component.msg = `Data fetched: ${response.data}`;
                })
                .catch(function (err) {
                    component.msg = `Failed fetching data.`;
                });
        }
        mounted(): void {
            this.fetchApi();
        }
    }
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
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
