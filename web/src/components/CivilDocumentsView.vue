<template>
<body>
   <b-card bg-variant="white" border-variant="white">   
       
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
        
        <b-overlay :show="loadingPdf" rounded="sm">  
            <b-card border-variant="white" :aria-hidden="loadingPdf ? 'true' : null"></b-card>
        </b-overlay>

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
                <template v-for="(f,index) in fields[fieldsTab]" v-slot:[`head(${f.key})`]="data">
                   <b v-bind:key="index" :class="f.headerStyle" > {{ data.label }}</b>
                </template>
                 <template v-for="(f,index) in fields[fieldsTab]" v-slot:[`cell(${f.key})`]="data" >
                   <span 
                        v-bind:key="index" 
                        v-b-hover="colHover"
                        v-on:click="(data.item.PdfAvail)&&(index==1)&&(activetab!='COURT SUMMARY')? 
                        openDocumentsPdf(data.item['Document ID']): 
                        ((index==0)&&(activetab=='COURT SUMMARY'))? openCourtSummaryPdf(data.item['Appearance ID']) : ''"
                        :class="(data.item.PdfAvail)&&(index==1)&&(activetab!='COURT SUMMARY')? 
                        (hoverCol==1 && hoverRow==data.item.Index)?'text-info bg-warning':'text-info'
                        : (index==0)&&(activetab=='COURT SUMMARY')? 
                        (hoverCol==0 && hoverRow==data.item.Index)?'text-info bg-warning':'text-info'
                        : f.cellStyle" 
                        style="white-space: pre-line"> {{ data.value }}
                    </span>
                </template>
            </b-table>
        </b-card>

   </b-card> 
</body>
</template>

<script lang="ts">
import { Component, Vue } from 'vue-property-decorator';
import { namespace } from 'vuex-class';
import CivilFileDocuments from '../store/modules/CivilFileDocuments';
const civilState = namespace('CivilFileDocuments');

enum fieldTab {Categories=0, Summary}

@Component
export default class CivilDocumentsView extends Vue {

    @civilState.State
    public civilFileDocument!: any

    public getDocuments(): void {
        if (this.civilFileDocument.fileNumber === undefined)
            this.civilFileDocument.fileNumber = this.$route.query.fileNumber;
        this.$http.get('api/files/civil/'+ this.civilFileDocument.fileNumber)
            .then(Response => Response.json(), err => console.log('error')        
            ).then(data => {
                this.documentsDetailsJson = data.document
                this.ExtractDocumentInfo()
            });
    }

    mounted () {         
        this.getDocuments();        
    }

    documentsDetailsJson;
    loadingPdf = false;

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

                docInfo["Category"] = jDoc.category
                if((this.categories.indexOf(docInfo["Category"]) < 0) && docInfo["Category"].length > 0) this.categories.push(docInfo["Category"])
                // ensure all documentSupport elements only have one row
                const docSupport: any = jDoc.documentSupport.length? jDoc.documentSupport[0]:'{}';
                docInfo["Act"] = (docSupport==={})? '': docSupport.actCd;
                docInfo["Document ID"] = jDoc.civilDocumentId;            
                docInfo["PdfAvail"] = jDoc.civilDocumentId? true : false 
                docInfo["Date Filed"] = jDoc.filedDt? jDoc.filedDt.split(" ")[0]: ' ';
                docInfo["Issues"] = jDoc.issue.length? this.ExtractIssues(jDoc.issue) : ' ';
                this.documents.push(docInfo);

            } else {                
                docInfo["Document Type"] = 'CourtSummary';
                docInfo["Appearance Date"] = jDoc.lastAppearanceDt.split(" ")[0]
                docInfo["Appearance ID"] = jDoc.imageId;
                this.summaryDocuments.push(docInfo);
                courtSummaryExists = true
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
        else{       
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
        this.loadingPdf = true
        // TODO: remove the hardcoded documentId once sample data has pdf
        documentId = 70
        const filename = 'doc'+documentId+'.pdf'
        // TODO: change to civil when a civil documentID is available 

        window.open(`api/files/document/${documentId}/${filename}?isCriminal=true`)
        this.loadingPdf = false
    }
    
    public openCourtSummaryPdf(appearanceId): void {

        this.loadingPdf = true
        // TODO: remove the hardcoded appearanceId once sample data has pdf
        appearanceId = 10098
        const filename = 'court summary'+appearanceId+'.pdf'

        window.open(`api/files/civil/court-summary-report/${appearanceId}/${filename}`)
        this.loadingPdf = false
    }
    
    public colHover(hovered, mouseEvent) {            
        hovered? this.hoverCol = mouseEvent.fromElement.cellIndex: this.hoverCol =-1;
    }

    public rowHover(row) {
        this.hoverRow = row.Index;
    }
    
}
</script>