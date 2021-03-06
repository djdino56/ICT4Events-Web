﻿using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Newtonsoft.Json;
using SharedModels.Logic;
using SharedModels.Models;

namespace ICT4Events_Web.Account
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var user = SiteMaster.CurrentUser();
            if (user != null)
            {
                lblNotLoggedIn.Visible = true;
                lblNotLoggedIn.Text = "Je bent al ingelogd.";
                loginForm.Visible = false;
            }
        }

        protected void LogIn(object sender, EventArgs e)
        {
            if (!IsValid) return;
            var email = Email.Text;

            var password = LogicCollection.UserLogic.GetHashedPassword(Password.Text);

            if (!LogicCollection.UserLogic.IsValidEmail(email))
            {
                errorLabel.Visible = true;
                errorLabel.Text = "Uw heeft een ongeldig emailadres ingevuld.";
                return;
            }

            var currentUser = LogicCollection.UserLogic.AuthenticateUser(email, password);
            if (currentUser == null)
            {
                errorLabel.Visible = true;
                errorLabel.Text = "Uw inloggegevens komen niet overeen met een bestaand account.";
                return;
            }

            //contains user object in JSON format
             var ticket = new FormsAuthenticationTicket(1, currentUser.Username, DateTime.Now,
             DateTime.Now.AddMinutes(30), RememberMe.Checked, JsonConvert.SerializeObject(currentUser));

            // cookie containing copy of ticket
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                Expires = ticket.Expiration,
                Path = FormsAuthentication.FormsCookiePath
            };

            Response.Cookies.Add(cookie);
            var str = Request["ReturnUrl"] ?? "/Timeline";
            Response.Redirect(str, true);
        }
    }
}