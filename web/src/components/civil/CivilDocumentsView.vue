<template>
<body>
     <b-card bg-variant="light" v-if= "!isMounted && !isDataValid">
        <b-overlay :show= "true"> 
            <b-card style="min-height: 100px;"/>                   
            <template v-slot:overlay>               
               <div> 
                    <loading-spinner/> 
                    <p id="loading-label">Loading ...</p>
               </div>                
            </template> 
      </b-overlay> 
    </b-card>

    <b-card bg-variant="light" v-if= "isMounted && !isDataValid">
        <b-card  style="min-height: 100px;">
            <span>This <b>File-Number '{{this.civilFileInformation.fileNumber}}'</b> doesn't exist in the <b>civil</b> records. </span>
        </b-card>
        <b-card>    
            <b-button variant="info" @click="navigateToLandingPage">Back to the Landing Page</b-button>
        </b-card>
    </b-card>

   <b-card  v-if= "isMounted && isDataValid">  
       
        <b-card bg-variant="light">
            <b-tabs active-nav-item-class="font-weight-bold text-uppercase text-info bg-light" pills >
                <b-tab 
                v-for="(tabMapping, index) in categories" 
                :key="index"                 
                :title="tabMapping"                 
                v-on:click="activetab = tabMapping" 
                v-bind:class="[ activetab === tabMapping ? 'active' : '' ]"
                ></b-tab>
            </b-tabs>
        </b-card>        
       
        <b-card/>
 
        <b-overlay :show="loadingPdf" rounded="sm">  
            <b-card bg-variant="light">           
                <b-table
                :items="FilteredDocuments"
                :fields="fields[fieldsTab]"
                :sort-by.sync="sortBy"
                :sort-desc.sync="sortDesc"
                :no-sort-reset="true"
                @row-hovered="rowHover"
                striped
                responsive="sm"
                >   
                    <template v-for="(field,index) in fields[fieldsTab]" v-slot:[`head(${field.key})`]="data">
                        <b v-bind:key="index" :class="field.headerStyle" > {{ data.label }}</b>
                    </template>
                    <template v-for="(field,index) in fields[fieldsTab]" v-slot:[`cell(${field.key})`]="data" >
                        <span 
                            v-bind:key= "index" 
                            v-b-hover= "colHover"                            
                            v-on:click= "cellClick(index, data)"
                            :class= "cellClass(field, index, data)"    
                            style= "white-space: pre-line"> {{ data.value }}
                        </span>
                    </template>
                </b-table>
            </b-card>
            <template v-slot:overlay>               
               <div> 
                    <loading-spinner/> 
                    <p id="Downloading-label">Downloading PDF file ...</p>
               </div>                
            </template> 
        </b-overlay>
   </b-card> 
</body>
</template>

<script lang="ts">
import { Component, Vue, Watch} from 'vue-property-decorator';
import { namespace } from 'vuex-class';
import '@store/modules/CivilFileInformation';
const civilState = namespace('CivilFileInformation');

enum fieldTab {Categories=0, Summary}

@Component
export default class CivilDocumentsView extends Vue {

    @civilState.State
    public civilFileInformation!: any

    @civilState.Action
    public UpdateCivilFile!: (newCivilFileInformation: any) => void

    public getDocuments(): void {
        
        this.$http.get('/api/files/civil/'+ this.civilFileInformation.fileNumber)
            .then(Response => Response.json(), err => {console.log('error');this.isMounted = true;}        
            ).then(data => {
                this.documentsDetailsJson = data.document
                this.ExtractDocumentInfo()
                if(this.documents.length)
                {
                    this.isMounted = true;
                    this.isDataValid = true;
                }
                else
                {
                    this.isMounted = true;
                }
            });
    }

    @Watch('$route', { immediate: false, deep: true })
    onUrlChange() {
        this.civilFileInformation.fileNumber = this.$route.params.fileNumber;
        this.UpdateCivilFile(this.civilFileInformation);
        location.reload();
    }

    mounted () { 
        this.civilFileInformation.fileNumber = this.$route.params.fileNumber
        this.UpdateCivilFile(this.civilFileInformation);        
        this.getDocuments();        
    }

    documentsDetailsJson;
    loadingPdf = false;
    isMounted = false;
    isDataValid = false

    activetab = 'ALL';            
    sortBy = 'Seq.';
    sortDesc = false;
    hoverRow =-1;
    hoverCol = 0;

    documents: any[] = [];
    summaryDocuments: any[] = [];
    categories: any=[]; 

    fieldsTab = fieldTab.Categories;

    fields = [ 
        [
            {key:'Seq.',           sortable:true,  headerStyle:'text-primary',  cellStyle:'text'},
            {key:'Document Type',  sortable:true,  headerStyle:'text-primary',  cellStyle:'text-muted'},
            {key:'Act',            sortable:false, headerStyle:'text',          cellStyle:'text-white bg-secondary'},
            {key:'Date Filed',     sortable:true,  headerStyle:'text-danger',   cellStyle:'text'},
            {key:'Issues',         sortable:false, headerStyle:'text',          cellStyle:'text-muted'}
        ],
        [
            {key:'Document Type',    sortable:false, headerStyle:'text-primary',    cellStyle:'text-info'},
            {key:'Appearance Date',  sortable:true, headerStyle:'text-danger',     cellStyle:'text'},
        ]  
        
    ];

    public cellClick(index, data)
    {
        if(data.item.PdfAvail && index==1 && this.activetab!='COURT SUMMARY')
        {
            this.openDocumentsPdf(data.item['Document ID']);
        }
        else if (index==0 && this.activetab=='COURT SUMMARY')
        {
            this.openCourtSummaryPdf(data.item['Appearance ID']);
        }         
    }

    public cellClass(field, index, data)
    {
        if(data.item.PdfAvail && index==1 && this.activetab!='COURT SUMMARY')
        {
            if(this.hoverCol==1 && this.hoverRow==data.item.Index) return 'text-white bg-warning'; else return 'text-info';            
        }
        else if(index==0 && this.activetab=='COURT SUMMARY')
        {
            if(this.hoverCol==0 && this.hoverRow==data.item.Index) return 'text-white bg-warning'; else return 'text-info';   
        }
        else 
            return field.cellStyle;
    }

    public navigateToLandingPage() {
        this.$router.push({name:'Home'})
    }
    
    public ExtractDocumentInfo(): void {
        let courtSummaryExists = false 
        for(const docIndex in this.documentsDetailsJson)
        {
            const docInfo = {}; 
            const jDoc =  this.documentsDetailsJson[docIndex];
            docInfo["Index"] = docIndex;
            if(jDoc.documentTypeCd != 'CSR') {
                docInfo["Seq."] = jDoc.fileSeqNo;
                docInfo["Document Type"] = jDoc.documentTypeDescription;
                docInfo["Concluded"] = jDoc.concludedYn;
                if((this.categories.indexOf("CONCLUDED") < 0) && docInfo["Concluded"].toUpperCase() =="Y") this.categories.push("CONCLUDED")        
                docInfo["Appearance Date"] = jDoc.lastAppearanceDt? jDoc.lastAppearanceDt.split(" ")[0]: ' ';
                if(new Date(docInfo["Appearance Date"]) > new Date() && this.categories.indexOf("SCHEDULED") < 0) this.categories.push("SCHEDULED")   

                docInfo["Category"] = jDoc.category;
                if((this.categories.indexOf(docInfo["Category"]) < 0) && docInfo["Category"].length > 0) this.categories.push(docInfo["Category"])
                // ensure all documentSupport elements only have one row
                const docSupport: any = jDoc.documentSupport.length? jDoc.documentSupport[0]:'{}';
                docInfo["Act"] = (docSupport==={})? '': docSupport.actCd;
                docInfo["Document ID"] = jDoc.civilDocumentId;            
                docInfo["PdfAvail"] = jDoc.imageId? true : false 
                docInfo["Date Filed"] = jDoc.filedDt? jDoc.filedDt.split(" ")[0]: ' ';
                docInfo["Issues"] = jDoc.issue.length? this.ExtractIssues(jDoc.issue) : ' ';
                this.documents.push(docInfo);

            } else {                
                docInfo["Document Type"] = 'CourtSummary';
                docInfo["Appearance Date"] = jDoc.lastAppearanceDt.split(" ")[0];
                docInfo["Appearance ID"] = jDoc.imageId;
                docInfo["PdfAvail"] = jDoc.imageId? true : false
                this.summaryDocuments.push(docInfo);
                courtSummaryExists = true;
            }
        } 
        
        this.categories.sort()
        if(courtSummaryExists) this.categories.push("COURT SUMMARY")
        this.categories.unshift("ALL")  
    }

    public ExtractIssues(issues) {
        let issueString =''; 
        for (const issue of issues)
        {
            issueString += issue.issueDsc + '\n';
        }
        return issueString
    }

    get FilteredDocuments() {

        if(this.activetab == 'COURT SUMMARY')
        {
            this.fieldsTab = fieldTab.Summary;
            return this.summaryDocuments;
        }
        else {       
            return this.documents.filter(doc => {                
                this.fieldsTab = fieldTab.Categories;
                if(this.activetab == 'CONCLUDED') {
                    if(doc["Concluded"] === "Y") return true; else return false;
                
                } else if(this.activetab == 'SCHEDULED') {
                    if(doc["Appearance Date"]){        
                        if(new Date(doc["Appearance Date"]) > new Date()) return true; else return false;
                    
                    } else {
                        return false
                    }  

                } else if ( this.activetab != 'ALL' )
                {
                    if (doc["Category"].toUpperCase() == this.activetab.toUpperCase()) return true;                                   
                                  
                    return false;                     
                }
                else
                {
                    return true;
                }
            }); 
        }       
    }    

    public openDocumentsPdf(documentId): void {
        this.loadingPdf = true;
        const filename = 'doc'+documentId+'.pdf';
        window.open(`/api/files/document/${documentId}/${filename}?isCriminal=false`)
        this.loadingPdf = false;
    }
    
    public openCourtSummaryPdf(appearanceId): void {

        this.loadingPdf = true;        
        const filename = 'court summary_'+appearanceId+'.pdf';
        window.open(`/api/files/civil/court-summary-report/${appearanceId}/${filename}`)
        this.loadingPdf = false;
    }
    
    public colHover(hovered, mouseEvent) {            
        hovered? this.hoverCol = mouseEvent.fromElement.cellIndex: this.hoverCol =-1;
    }

    public rowHover(row) {
        this.hoverRow = row.Index;
    }
    
}
</script>

<style scoped>
 .card {
        border: white;
    }

</style>