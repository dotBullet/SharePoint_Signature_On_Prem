import * as React from 'react';
import styles from './AccessToken.module.scss';
import { IAccessTokenProps } from './IAccessTokenProps';
import { escape } from '@microsoft/sp-lodash-subset';
import { UrlQueryParameterCollection } from '@microsoft/sp-core-library';
import * as jQuery from "jquery";
import * as bootstrap from "bootstrap";

import {
  MessageBar,
  MessageBarType,
  Button,
  PrimaryButton,
  DefaultButton

} from 'office-ui-fabric-react';

//require('../../../node_modules/@fontawesome/fontawesome-free/css/all.min.css');
//require('../../../node_modules/bootstrap/dist/css/bootstrap.min.css');
//require('../../node_modules/bootoast/dist/bootoast.min.css');
//require('../../node_modules/bootstrap/dist/css/bootstrap.min.css');
//var bootoast: any = require('../../../node_modules/bootoast/dist/bootoast.min.js');

interface WCFResponse {
  [key: string]: any
}
export interface IRSSPWebPartState {
  ttl: number,
  token: string,
  error_message: string,
  error_desc: string,
  initial: boolean

}

export default class AccessToken extends React.Component<IAccessTokenProps, IRSSPWebPartState> {
  rsspCalls: any;

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

  private getAccessToken() {
    var queryParameters = new UrlQueryParameterCollection(window.location.href);

    if (this.getCookie("access_token") == "") {
      console.log("before fetch");
      let that = this;
      fetch(`/_vti_bin/FileUtils/Services.svc/GetAccessToken/${queryParameters.getValue('code')}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
          // 'Content-Type': 'application/x-www-form-urlencoded',
        }
      })
        .then(res => {
          return res.json()
        }
        ).then(function (data) {
          var r: WCFResponse = data;
          var rssp = JSON.parse(r.Message);
          console.log(rssp);
          //dupa apelul serviciului web se salveaza token=ul, data generarii si durata de viata in cookie dar si in stare

          if (rssp.error != undefined && rssp.error != null && rssp.error != "") {
            that.setState({
              token: "",
              ttl: 0,
              error_message: rssp.error,
              error_desc: rssp.error_description,
              initial: false

            });
          }
          else {
            console.log("setarea: " + rssp.accsess_token);
            that.setCookie("token", rssp.access_token, rssp.expires_in);//se iau valorile din raspunsul serv web - token si probabil 3600
            that.setCookie("ttl", rssp.expires_in, rssp.expires_in);//3600 la ambele, sau ce vine din serviciul web
            that.setCookie("creationDate", new Date(), rssp.expires_in);//3600 in loc de 10
            console.log("cookie setat: " + rssp.accsess_token);
            //acelasi token si durata de viata ca mai sus se salveaza in React state si e folosita mai jos de messagebar
            that.setState({
              token: rssp.access_token,
              ttl: rssp.expires_in,
              error_message: "",
              error_desc: "",
              initial: false

            });
          }

          //se face un interval care la 5 secunde actualizeaza informatia dein webpart

          if (r.Result > 0)
            that.setState({
              token: "",
              ttl: 0,
              error_message: r.Message,
              error_desc: r.Message,
              initial: false

            });

        }).catch(function (error) {

          console.log("Eroare serviciu web!: " + error);

        });
    }
  }

  private updateState() {
    console.log("UpdateState");
    let currentToken = this.getCookie("token");
    let creationDate = new Date(this.getCookie("creationDate"));
    let currentDate = new Date();
    var dif = currentDate.getTime() - creationDate.getTime();
    var Seconds_from_T1_to_T2 = dif / 1000;
    var Seconds_Between_Dates = Math.abs(Seconds_from_T1_to_T2);
    //alert(Seconds_Between_Dates);
    let ttl_seconds = this.state.ttl - Seconds_Between_Dates;
    this.setState({
      token: currentToken,
      ttl: ttl_seconds,
      initial: false
    });
  }

  constructor(props: IAccessTokenProps, state: IRSSPWebPartState) {
    super(props);

    //e nevoie de aceste bind-uri altfel nu o sa poata fi apelate functiile
    this.setCookie = this.setCookie.bind(this);
    this.getCookie = this.getCookie.bind(this);
    this.updateState = this.updateState.bind(this);
    this.getAccessToken = this.getAccessToken.bind(this);
    //APEL SERVICIU WEB GetAccessToken AICI si in then, daca a fost cu succes se fac operatiile de mai jos

    this.state = {
      token: "",
      ttl: 0,
      error_message: "Se verifica existenta si se incearca obtinerea token-ului.",
      error_desc: "Se incearca in 3 secunde.",
      initial: true
    };

    setInterval(this.updateState, 5000);
    setTimeout(this.getAccessToken, 3000);
  }


  public render(): React.ReactElement<IAccessTokenProps> {
    /*Apel catre serviciu web VS care imi intoarce acces_token
    Citire din URL*/

    try {
    } catch (exception) {
      console.log(exception);
    }
    return (

      <div className={styles.accessToken} >
        <div className={styles.container}>
          {this.state.error_message == "" &&
            <MessageBar isMultiline={true}
              messageBarType={this.state.token != "" ? (this.state.ttl < 10 ? MessageBarType.warning : MessageBarType.success) : MessageBarType.error}>
              {this.state.token != "" && <div>Token-ul:<b> {this.state.token}</b> va expira in <b>{this.state.ttl}</b> secunde</div>}

              {this.state.token == "" && <div>Token expirat sau inexistent <PrimaryButton onClick={() => {
                window.location.href = "https://msign-test.transsped.ro/csc/v0/oauth2/authorize?response_type=code&client_id=msdiverse&redirect_uri=http://localhost:8080/&scope=service";
              }} text="Obtinere token nou"></PrimaryButton></div>}
            </MessageBar>
          }
          {this.state.error_message != "" &&
            <MessageBar isMultiline={true}
              messageBarType={this.state.initial ? MessageBarType.info : MessageBarType.error}>
              {this.state.error_message}<br />
              {this.state.error_desc}<br />
              {this.state.token == "" && <div>Token expirat sau inexistent <PrimaryButton onClick={() => {
                window.location.href = "https://msign-test.transsped.ro/csc/v0/oauth2/authorize?response_type=code&client_id=msdiverse&redirect_uri=http://localhost:8080/&scope=service";

              }} text="Obtinere token nou"></PrimaryButton></div>}
            </MessageBar>
          }

        </div>
      </div>

    );
  }
}
