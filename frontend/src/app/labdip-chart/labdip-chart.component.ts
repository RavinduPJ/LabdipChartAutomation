import { Component, OnInit,AfterViewInit,ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { LabdipChartVM } from '../model/labdip.chart.vm';
import { LabdipChartKeyinDialogComponent } from './labdip-chart-keyin-dialog/labdip-chart-keyin-dialog.component';
import { LabdipChartKeyinData } from '../model/labdip.chart.keyin.data';
import * as XLSX from "xlsx";
import { MatSort, Sort}  from '@angular/material/sort';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { NotifierService } from '../services/notifier.service';
import { HttpServiceService } from '../services/http-service.service';
import { AutomationRequest } from '../model/automation.request';
import { LabdipChartRequest } from '../model/labdipchart.request';
import { EnvService } from '../services/env.service';


@Component({
  selector: 'app-labdip-chart',
  templateUrl: './labdip-chart.component.html',
  styleUrls: ['./labdip-chart.component.scss']
})
export class LabdipChartComponent implements AfterViewInit { 
  
  displayedColumns : string []=['keyin','index',
                                'division',
                                'season',
                                'category',
                                'program',
                                'styleNoIndividual',
                                'gmtDescription',
                                'gmtColor',
                                'nrf',
                                'colorCode',
                                'packCombination',
                                'palcementName',
                                'bomSelection',
                                'itemName',
                                'supplierName',
                                'rmColor',   
                                'colorDyeingTechnic',  
                                'rmColorRef',
                                'garmentWay',
                                'fbNumber',
                                'materialType'];
  dataSource = new MatTableDataSource<LabdipChartVM>();
  pageSize : number = 5;
  selectedFileToProcess:File;
  recordCountZero : boolean = false;
  notNewFileSelect : boolean = true;

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort ; 

  constructor(public dialog: MatDialog, 
              private _liveAnnouncer: LiveAnnouncer,
              private http:HttpClient,
              private _notifierService:NotifierService,
              private _http:HttpServiceService,
              private _envService:EnvService) { 
   /*  this.selectedFileToProcess = new File();
    this.paginator = null;
    this.sort = null; */
  }
 

  ngAfterViewInit() {      
    this.dataSource.paginator = this.paginator;  
    this.dataSource.sort = this.sort;     
  }

  processLabdipFile()
  {  
    console.log("click process event");
    this.uploadFile(this.selectedFileToProcess);
  }

  onFileSelected(event:any){    
    this.selectedFileToProcess = event.target.files[0];
    if(this.selectedFileToProcess){
      this.notNewFileSelect = false; 
      this.recordCountZero = false;  
      this.dataSource = new MatTableDataSource<LabdipChartVM>(); 
      this.ngAfterViewInit();       
    }
  }

  uploadFile(file : File)
  {
    let fileUpload = file;
    const formData = new FormData();
    formData.append('file',fileUpload,fileUpload.name);

    console.log(formData);
    //http://localhost:5000/api/labdip
    //http://18.208.148.189:5700/api/labdip
    
    this.http.post(`${this._envService.apiUrl}labdip`,formData /*,{ reportProgress : true, observe : 'events'}*/).subscribe(result => {       
       if(result)
        {
          this.dataSource.data = <LabdipChartVM[]> result; 
          if(this.dataSource.data.length > 0) 
          {
              this.notNewFileSelect = true;
              this.recordCountZero = true;
              this._notifierService.showNotification("success",'OK');
              this.pageSize = this.dataSource.data.length;
          }
            console.log(result);
            
        }
      else
        {
          this._notifierService.showNotification("success",'OK');
        }  
     /*  if(event.type == HttpEventType.UploadProgress)
      {

      }
      else if(event.type == HttpEventType.Response)
      {
        .subscribe(result=> {

        });
      } */
    });
  /*   this._http.labdipChartProcess(this.prepareRequest(this.selectedFileToProcess)).subscribe(data=>{
      console.log(data);
    }) */

  }

  prepareRequest(selectedFile:File):AutomationRequest<LabdipChartRequest>
  {
    let fileUpload = selectedFile;
    let formData = new FormData();
    formData.append('file',fileUpload,fileUpload.name);   

    let labdipChartRequest : LabdipChartRequest = {
      file : formData
    }
    console.log(labdipChartRequest);
    var req = new AutomationRequest(labdipChartRequest);
    console.log(req);
    return new AutomationRequest(labdipChartRequest);
  }


  // editContact(element)
  editContact(row:any){
    console.log(row);
  }

  announceSortChange(sortState: Sort) {
    // This example uses English messages. If your application supports
    // multiple language, you would internationalize these strings.
    // Furthermore, you can customize the message to add additional
    // details about the values being sorted.
    if (sortState.direction) {
      this._liveAnnouncer.announce(`Sorted ${sortState.direction}ending`);
    } else {
      this._liveAnnouncer.announce('Sorting cleared');
    }
  }

  openDialog(element:any): void {
    const dialogRef = this.dialog.open(LabdipChartKeyinDialogComponent,{
      width: '600px',
      data: this.prepareKeyInData(element),      
      height:'auto',      
    });
    dialogRef.afterClosed().subscribe(result => {     
     if(result)
     {
      if(result.applyToAll)
      {
        this.dataSource.data.forEach(ele=> {
          ele.program = result.program;
          ele.packCombination = result.packCombination;
          ele.rmColorRef = result.rmColorRef;
        });
      }
      else
      {
        this.dataSource.data.filter(p => p.index==result.index).forEach(ele=>{
          ele.program = result.program;
          ele.packCombination = result.packCombination;
          ele.rmColorRef = result.rmColorRef;
        });
      }  
     }      
    });
  }

  prepareKeyInData(element:any):LabdipChartKeyinData
  {  
    let labdipChartKeyData : LabdipChartKeyinData = {
      index:element.index,
      program : element.program,
      packCombination : element.packCombination,
      rmColorRef : element.rmColorRef,
      applyToAll : true
    }
    return labdipChartKeyData
  }

  exportexcel(tableId: string, name?: string)
  {
    /* pass here the table id */
    let element = document.getElementById("labdipChartTable");
    const ws: XLSX.WorkSheet =XLSX.utils.table_to_sheet(element);
 
    /* generate workbook and add the worksheet */
    const wb: XLSX.WorkBook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');
 
    /* save to file */  
    let timeSpan = new Date().toISOString();
    let prefix = name || "ExportResult";
    let fileName = `${prefix}-${timeSpan}`;
    XLSX.writeFile(wb,  `${fileName}.xlsx`); 
  }

}





