import { override } from '@microsoft/decorators';
import { UrlQueryParameterCollection } from '@microsoft/sp-core-library';
import { SPComponentLoader } from '@microsoft/sp-loader';
declare var SP: any;

import { SPHttpClient, SPHttpClientResponse, SPHttpClientConfiguration } from '@microsoft/sp-http';

import {
  BaseListViewCommandSet,
  Command,
  IListViewCommandSetListViewUpdatedParameters,
  IListViewCommandSetExecuteEventParameters
} from '@microsoft/sp-listview-extensibility';
import { Dialog } from '@microsoft/sp-dialog';

import {
  MessageBar,
  MessageBarType,
  Button,
  PrimaryButton,
  DefaultButton

} from 'office-ui-fabric-react';

export interface IRSSPWebPartState {
  ttl: number,
  token: string
}

export interface IHelloWorldCommandSetProperties {
  // This is an example; replace with your own properties
  sampleTextOne: string;
}

interface WCFResponse {

  [key: string]: any

}

const LOG_SOURCE: string = 'HelloWorldCommandSet';

export default class HelloWorldCommandSet extends BaseListViewCommandSet<IHelloWorldCommandSetProperties> {


  private setCookie(cname, cvalue, exseconds) {
    var d = new Date();
    d.setTime(d.getTime() + (exseconds * 1000));
    var expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
  }

  private getCookie(cname) {
    var name = cname + "=";
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
      var c = ca[i];
      while (c.charAt(0) == ' ') {
        c = c.substring(1);
      }
      if (c.indexOf(name) == 0) {
        return c.substring(name.length, c.length);
      }
    }
    return "";
  }

  private checkCookie() {
    var username = this.getCookie("username");
    if (username != "") {
      alert("Welcome again " + username);
    } else {
      username = prompt("Please enter your name:", "");
      if (username != "" && username != null) {
        this.setCookie("username", username, 365);
      }
    }
  }


  @override
  public onListViewUpdated(event: IListViewCommandSetListViewUpdatedParameters): void {
    const compareOneCommand: Command = this.tryGetCommand('COMMAND_1');
    if (compareOneCommand) {
      // This command should be hidden unless exactly one row is selected.
      compareOneCommand.visible = event.selectedRows.length === 1;
    }
  }
  //butonul de semnare

  @override
  public onExecute(event: IListViewCommandSetExecuteEventParameters): void {
    let row = event.selectedRows[0];
    switch (event.itemId) {
      case 'COMMAND_1':
        var queryParameters = new UrlQueryParameterCollection(window.location.href);
        let idFile = row.getValueByName("ID");
        let hash = "";
        fetch(`/_vti_bin/FileUtils/Services.svc/GetHashPDF/${idFile}`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
            // 'Content-Type': 'application/x-www-form-urlencoded',
          }
        })
          .then(res => {
            return res.json()
          })
          .then(function (data) {
            var r: WCFResponse = data;
            //alert(r.Message);
            if (data.Result != 0)
              console.log("Eroare generare Hash: " + data.Message);
            else {
              hash = data.Message;
              console.log(data.Result);
              window.open("https://msign-test.transsped.ro/csc/v0/oauth2/authorize?response_type=code&client_id=msdiverse&redirect_uri=http://localhost:8080/&scope=credential&credentialID=A122E0EFAF8C75AE0B3091183E9641AAD70C97DF&numSignatures=1&hash=" + hash + "&state="+ hash +";" + idFile, "_blank");
            }
            //success or warning
            //if (r.Result < 2) { setTimeout(function () { window.location.reload() }, 1000) };
          }).catch(function (error) {
            alert("Eroare serviciu web! -- Hash");
          });

        try {
        } catch (exception) {
          console.log(exception);
        }
        return;

      default:
        throw new Error('Unknown command');
    }

  }

}
