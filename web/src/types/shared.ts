export interface DocumentData {
    appearanceDate?: string;  
    appearanceId?: string;
    courtClass?: string;  
    courtLevel: string;
    dateFiled?: string;
    documentDescription?: string;
    documentId?: string;
    fileId?: string;
    fileNumberText: string;
    location: string;
    partId?: string;
    partyName?: string;
    profSeqNo?: string;
    correlationId?: string;
}

export enum CourtDocumentType {
    Civil,
    ProvidedCivil,
    CSR,
    Criminal,
    ROP,
    CriminalZip,
    CivilZip
  }
  