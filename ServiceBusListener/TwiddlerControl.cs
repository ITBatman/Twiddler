using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.ServiceBus;
using System.ServiceModel;
using ServiceBusListener.Router;
using System.Net;
using System.Net.Http;
using ServiceBusListener.Utilities;

namespace ServiceBusListener
{

  public partial class TwiddlerControl : UserControl
  {
    //namespace manager is for creating queues, topics, and subscriptions
    ServiceHost _serviceHost = null;

    Twilio.TwilioRestClient twilioclient = null;

    public TwiddlerControl()
    {
      InitializeComponent();
      this.lblStatusMessage.BackColor = Color.Yellow;
      this.lblStatusMessage.Text = "Waiting to start";

      //allows the ServiceHost to set a message on the form
      ServiceFormBridge.ErrorMessage = this.txtErrorMsg;

      //Initialize the form after it's been loaded
      this.Load += ServiceBusListenerParameters_Load;


    }

    private void StartServiceBusListener()
    {
      try
      {
        if (_serviceHost == null)
        {
          this.DisableControls(true);

          //transfer data from the form, to an area that the service can retrieve from
          ServiceFormBridge.NameSpaceName = this.txtServiceNameSpace.Text;
          ServiceFormBridge.EndPointName = this.txtEndPointName.Text;

          ServiceFormBridge.FullServiceBusNamespaceURL = this.lblFullServiceBusNamespace.Text;
          ServiceFormBridge.DestinationWebSite = this.txtRedirectTo.Text;
          ServiceFormBridge.DevelopersPhoneNumber = this.txtDevelopersPhone.Text;

          this.txtErrorMsg.Visible = false;

          //setup the service host - I used code so that I can use values directly from the form
          _serviceHost = new ServiceHost(typeof(RoutingService));

          var NoCredentialBinding = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.None, RelayClientAuthenticationType.None);

          _serviceHost.AddServiceEndpoint(
             typeof(IRoutingService), NoCredentialBinding,
             ServiceBusEnvironment.CreateServiceUri("sb", this.txtServiceNameSpace.Text, this.txtEndPointName.Text))
              .Behaviors.Add(new TransportClientEndpointBehavior
              {
                TokenProvider = TokenProvider.CreateSharedSecretTokenProvider(this.txtIssuerName.Text, this.txtIssureKey.Text)
              });

          try
          {

            this.Cursor = Cursors.AppStarting;
            _serviceHost.Open();

            //give an indication that we are listening
            this.lblStatusMessage.BackColor = Color.LightGreen;
            this.lblStatusMessage.Text = "Service Bus Listener has started successfully";

          }
          catch (Exception ex)
          {
            this.lblStatusMessage.Text = "Error while attempting a connection to SB";
            ServiceFormBridge.SetErrorMessage(string.Format("Error:{0}", ex.Message));
          }
          finally
          {
            this.Cursor = Cursors.Default;
          }
        }
        else
        {
          this.lblStatusMessage.Text = "Attempt to start the Service Bus Listener, but it's already been started!";
          this.lblStatusMessage.BackColor = Control.DefaultBackColor;
        }
      }
      catch (Exception ex)
      {
        //fidder does not display or show exceptions, so all methods have to be wrapped and display any errors with a MessageBox
        ex.DisplayException();
      }

    }

    private void ShutDownServiceBus()
    {
      try
      {
        if (_serviceHost != null)
        {
          _serviceHost.Close();
          _serviceHost = null;
        }
        this.DisableControls(false);
        this.lblStatusMessage.BackColor = Control.DefaultBackColor;
        this.lblStatusMessage.Text = "Service Bus Listener successfully closed";
      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
    }

    private bool ValidateSBTabFields()
    {
      bool Result = true;
      try
      {

        this.txtErrorMsg.Text = string.Empty;
        this.txtServiceNameSpace.BackColor = System.Drawing.SystemColors.Window;
        this.txtEndPointName.BackColor = System.Drawing.SystemColors.Window;
        this.txtController.BackColor = System.Drawing.SystemColors.Window;
        this.txtAction.BackColor = System.Drawing.SystemColors.Window;
        this.txtIssuerName.BackColor = System.Drawing.SystemColors.Window;
        this.txtIssureKey.BackColor = System.Drawing.SystemColors.Window;


        if (this.txtServiceNameSpace.Text.Trim().Length == 0)
        {
          ServiceFormBridge.SetErrorMessage("Service Namespace cannot be blank");
          this.txtServiceNameSpace.BackColor = System.Drawing.SystemColors.ActiveBorder;
          Result = false;
        }

        if (this.txtEndPointName.Text.Trim().Length == 0)
        {
          ServiceFormBridge.SetErrorMessage("Endpoint name cannot be blank");
          this.txtEndPointName.BackColor = System.Drawing.SystemColors.ActiveBorder;
          Result = false;
        }

        if (this.txtController.Text.Trim().Length == 0)
        {
          ServiceFormBridge.SetErrorMessage("Controller name cannot be blank");
          this.txtController.BackColor = System.Drawing.SystemColors.ActiveBorder;
          Result = false; 
        }

        if (this.txtAction.Text.Trim().Length == 0)
        {
          ServiceFormBridge.SetErrorMessage("Action name cannot be blank");
          this.txtAction.BackColor = System.Drawing.SystemColors.ActiveBorder;
          Result = false;
        }

        if (this.txtIssuerName.Text.Trim().Length == 0)
        {
          ServiceFormBridge.SetErrorMessage("Issuer name cannot be blank");
          this.txtIssuerName.BackColor = System.Drawing.SystemColors.ActiveBorder;
          Result = false;
        }

        if (this.txtIssureKey.Text.Trim().Length == 0)
        {
          ServiceFormBridge.SetErrorMessage("Issuer key cannot be blank");
          this.txtIssureKey.BackColor = System.Drawing.SystemColors.ActiveBorder;
          Result = false;
        }

        if (Result == false)
          return Result;
                                        
        if (ValidateURL(this.txtRedirectTo.Text) == false)
        {
          ServiceFormBridge.SetErrorMessage("URL is not valid.  Must be Http://<<LocalServices>> or https://<<LocalServer>>");
          Result = false;
          return Result;
        }

        //alter local host with something that will always work with fiddler
        Uri RedirectURI = new Uri(this.txtRedirectTo.Text);
        if (RedirectURI.Host.ToLower() == "localhost")
          this.txtRedirectTo.Text = RedirectURI.AbsoluteUri.Replace("localhost", "ipv4.fiddler");

        if (ValidateHost(RedirectURI) == false)
        {
          ServiceFormBridge.SetErrorMessage(string.Format("Validation of the Host failed - no host is available at {0}", this.txtRedirectTo.Text));
          Result = false;
          return Result;
        }

        //strip the last slash
        if (this.txtRedirectTo.Text.TrimEnd().EndsWith(@"/") == false)
          this.txtRedirectTo.Text = string.Concat(this.txtRedirectTo.Text.TrimEnd(), @"/");

        Result = true;
      }
      catch (Exception ex)
      {
        ex.DisplayException();
        Result = false;
      }
      return Result;
    }

    private void DisableControls(bool pFreeze)
    {
      try
      {
        this.txtServiceNameSpace.Enabled = !pFreeze;
        this.txtIssuerName.Enabled = !pFreeze;
        this.txtIssureKey.Enabled = !pFreeze;
        this.txtRedirectTo.Enabled = !pFreeze;
        this.txtEndPointName.Enabled = !pFreeze;
        this.txtController.Enabled = !pFreeze;
        this.txtAction.Enabled = !pFreeze;

        this.txtDevelopersPhone.Enabled = pFreeze;
        this.btnClickToCall.Enabled = pFreeze;
      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
    }

    private void txtServiceNameSpace_TextChanged(object sender, EventArgs e)
    {
      SetFullServiceNamespace();
    }

    private void SetFullServiceNamespace()
    {
      try
      {
        if (string.IsNullOrWhiteSpace(this.txtServiceNameSpace.Text))
          this.lblFullServiceBusNamespace.Text = string.Empty;
        else
        {
          this.lblFullServiceBusNamespace.Text = string.Format(@"http://{0}.servicebus.windows.net/{1}/api/{2}/{3}", this.txtServiceNameSpace.Text, this.txtEndPointName.Text, this.txtController.Text, this.txtAction.Text);
          this.lblSampleRewrite.Text = string.Format(@"{0}api/{1}/{2}", this.txtRedirectTo.Text, this.txtController.Text, this.txtAction.Text);
        }
      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
    }

    private void ServiceBusListenerParameters_Load(object sender, EventArgs e)
    {
      try
      {
        this.txtServiceNameSpace.Text = Properties.Settings.Default.ServiceNameSpace;
        this.txtIssuerName.Text = Properties.Settings.Default.IssuerName;
        this.txtIssureKey.Text = Properties.Settings.Default.IssuerKey;
        this.txtRedirectTo.Text = Properties.Settings.Default.RedirectTo;
        this.txtEndPointName.Text = Properties.Settings.Default.EndPointName;
        this.txtController.Text = Properties.Settings.Default.Controller;
        this.txtAction.Text = Properties.Settings.Default.Action;
        this.txtAccountSID.Text = Properties.Settings.Default.AccountSID;
        this.txtAuthToken.Text = Properties.Settings.Default.AccountAuthToken;
        this.txtDevelopersPhone.Text = Properties.Settings.Default.DevelopersPhoneNumber;
        SetFullServiceNamespace();
        SetTwilioErrorMessage(string.Empty);
        this.ParentForm.FormClosing += ParentForm_FormClosing;
      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
    }

    void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      try
      {
        //if parent form is closing , this is an attempt to shutdown the SB cleanly
        this.ParentForm.FormClosing -= ParentForm_FormClosing;
        ShutDownServiceBus();
      }
      catch
      {
#if DEBUG
        throw;
#endif
      }
    }


    private void SaveSettings()
    {
      try
      {
        Properties.Settings.Default.ServiceNameSpace = this.txtServiceNameSpace.Text;
        Properties.Settings.Default.IssuerName = this.txtIssuerName.Text;
        Properties.Settings.Default.IssuerKey = this.txtIssureKey.Text;
        Properties.Settings.Default.RedirectTo = this.txtRedirectTo.Text;
        Properties.Settings.Default.EndPointName = this.txtEndPointName.Text;
        Properties.Settings.Default.Controller = this.txtController.Text;
        Properties.Settings.Default.Action = this.txtAction.Text;
        Properties.Settings.Default.AccountSID = this.txtAccountSID.Text;
        Properties.Settings.Default.AccountAuthToken = this.txtAuthToken.Text;
        Properties.Settings.Default.DevelopersPhoneNumber = this.txtDevelopersPhone.Text;
        Properties.Settings.Default.Save();
      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
    }

    private void btnSaveSettings_Click(object sender, EventArgs e)
    {
      try
      {
        SaveSettings();
        MessageBox.Show("Parameters have been saved successfully!", "Setting Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
    }

    private bool ValidateURL(string pURL)
    {
      bool Result = false;
      try
      {
        Uri ValidURL = new Uri(pURL);
        Result = ValidURL.IsWellFormedOriginalString();
        if (Result)
          Result = ValidURL.Scheme.ToLower() == "http" || ValidURL.Scheme.ToLower() == "https";
      }
      catch (Exception ex)
      {
        ServiceFormBridge.SetErrorMessage(ex.Message);
#if DEBUG
        throw;
#endif
      }
      return Result;
    }

    /// <summary>
    /// A very basic attempt to determine if the local host is accessible - need help here!
    /// </summary>
    /// <param name="pUrl"></param>
    /// <returns></returns>
    private bool ValidateHost(Uri pUrl)
    {
      bool Result = false;
      try
      {
        this.Cursor = Cursors.AppStarting;
        HttpWebResponse response = null;
        try
        {

          string HostOnly = string.Empty;
          //ensure we have a trailing slash before rewriting the URL
          if (pUrl.AbsolutePath == @"/")
            HostOnly = pUrl.AbsoluteUri.ToString().ToLower();
          else
            HostOnly = pUrl.AbsoluteUri.ToString().Replace(pUrl.AbsolutePath, "").ToLower();

          //sending a request with ipv4.fiddler does not work to validate a local host, converting it back here locally to localhost
          if (HostOnly.Contains("ipv4.fiddler"))
            HostOnly = HostOnly.ToLower().Replace("ipv4.fiddler", "localhost");

          HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HostOnly);

          request.UserAgent = "Fiddler/4.4.2.4 beta (.NET 4.0.30319.17929; WinNT 6.1.7601 SP1; en-US; 4xAMD64)";
          request.Headers.Set(HttpRequestHeader.Pragma, "no-cache");
          request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US");

          //issuing a basic get to the host, and see if it returns OK status
          response = (HttpWebResponse)request.GetResponse();

          if (response != null && response.StatusCode == HttpStatusCode.OK)
          {
            Result = true;
          }

        }
        catch (WebException ex)
        {
          ServiceFormBridge.SetErrorMessage(string.Format("Web Exception using URL: {0}." + Environment.NewLine + "Error: {1}", pUrl.AbsoluteUri, ex.Message));
        }
        catch (Exception ex)
        {
          ServiceFormBridge.SetErrorMessage(string.Format("General Exception received using URL: {0}." + Environment.NewLine + "Error: {1}", pUrl.AbsoluteUri, ex.Message));
        }
        finally
        {
          if (response != null) response.Close();
        }
      }
      catch (Exception ex)
      {
        ServiceFormBridge.SetErrorMessage(string.Format("Unable to open a channel using URL: {0}." + Environment.NewLine + "Error: {1}", pUrl.AbsoluteUri, ex.Message));
        Result = false;
      }
      finally
      {
        this.Cursor = Cursors.Default;
      }

      return Result;

    }

    private void btnStartListening_CheckedChanged(object sender, EventArgs e)
    {
      if (this.btnStartListening.Checked)
      {
        if (ValidateSBTabFields() == false)
        {
          this.btnStopListening.Checked = true;
          return;
        }
        else
          StartServiceBusListener();
      }

    }

    private void btnStopListening_CheckedChanged(object sender, EventArgs e)
    {
      if (this.btnStopListening.Checked)
        ShutDownServiceBus();
    }

    private void btnSaveNameSpaceToClipBoard_Click(object sender, EventArgs e)
    {
      System.Windows.Forms.Clipboard.SetText(this.lblFullServiceBusNamespace.Text);
    }

    private void btnSendURLToClipBoard_Click(object sender, EventArgs e)
    {
      System.Windows.Forms.Clipboard.SetText(this.lblSampleRewrite.Text);
    }

    private void btnGetPhoneNbrs_Click(object sender, EventArgs e)
    {

      if (ValidateTwilioNumberTabFields() == false)
        return;
      
      try
      {
                
        this.Cursor = Cursors.AppStarting;

        twilioclient = new Twilio.TwilioRestClient(this.txtAccountSID.Text.Trim(), this.txtAuthToken.Text.Trim());

        var PhoneNumbersOnAccount = twilioclient.ListIncomingPhoneNumbers();

        if (PhoneNumbersOnAccount != null && PhoneNumbersOnAccount.IncomingPhoneNumbers.Count() > 0)
          this.incomingPhoneNumberBindingSource.DataSource = PhoneNumbersOnAccount.IncomingPhoneNumbers.OrderBy(pPhone => pPhone.FriendlyName);
        else
        {
          this.incomingPhoneNumberBindingSource = null;
          MessageBox.Show("Unable to retrieve phone numbers associated with the account.  Please login to Twilio and ensure Account, and Token is correct", "Twilio API error", MessageBoxButtons.OK);
        }
      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
      finally
      {
        twilioclient = null;
        this.Cursor = Cursors.Default;
      }
    }

    //could have used validators, but then I would also need to enumerate controls for empty/null values - this is simple
    private bool ValidateTwilioNumberTabFields()
    {
      bool Result = true;
      this.txtAccountSID.BackColor = System.Drawing.SystemColors.Window;
      this.txtAuthToken.BackColor = System.Drawing.SystemColors.Window;
      SetTwilioErrorMessage(string.Empty);
      
      if (this.txtAccountSID.Text.Trim().Length == 0)
      {
        SetTwilioErrorMessage("Twilio Account ID cannot be blank");
        this.txtAccountSID.BackColor = System.Drawing.SystemColors.ActiveBorder;
        Result = false;
      }

      if (this.txtAuthToken.Text.Trim().Length == 0)
      {
        SetTwilioErrorMessage("Twilio Authentication Token cannot be blank");
        this.txtAuthToken.BackColor = System.Drawing.SystemColors.ActiveBorder;
        Result = false;
      }

      
      return Result;
    }

    //really crude, but quick and effecient
    private void SetTwilioErrorMessage(string pErrorMessage)
    {
      if (string.IsNullOrWhiteSpace(pErrorMessage) )
      {
        this.txtTwilioErrorMessage.Text = string.Empty;
        this.txtTwilioErrorMessage.Visible = false;
      }
      else
      {
        this.txtTwilioErrorMessage.Text += pErrorMessage;
        this.txtTwilioErrorMessage.Visible = true;
      }
    }

    private void btnSaveChanges_Click(object sender, EventArgs e)
    {

      //reset first
      SetTwilioErrorMessage(string.Empty);

      if (this.lstPhoneNumbers.Items.Count == 0)
      {
        SetTwilioErrorMessage("You must have at least one Twilio Number in the list box before you can update - Use GetTwilioNumbers");
        return;
      }
            
      //will gather all errors before displaying      
      List<Twilio.RestException> APIExceptions = new List<Twilio.RestException>();
      try
      {
        this.Cursor = Cursors.AppStarting;

        twilioclient = new Twilio.TwilioRestClient(this.txtAccountSID.Text.Trim(), this.txtAuthToken.Text.Trim());

        Twilio.PhoneNumberOptions PhoneNumberOptions = new Twilio.PhoneNumberOptions();

        foreach (var phoneNumber in this.lstPhoneNumbers.Items.Cast<Twilio.IncomingPhoneNumber>())
        {

          PhoneNumberOptions.VoiceUrl = phoneNumber.VoiceUrl;
          PhoneNumberOptions.VoiceMethod = phoneNumber.VoiceMethod;

          PhoneNumberOptions.StatusCallback = phoneNumber.StatusCallback;
          PhoneNumberOptions.StatusCallbackMethod = phoneNumber.StatusCallbackMethod;

          PhoneNumberOptions.SmsUrl = phoneNumber.SmsUrl;
          PhoneNumberOptions.SmsMethod = phoneNumber.SmsMethod;

          PhoneNumberOptions.SmsFallbackUrl = phoneNumber.SmsFallbackUrl;
          PhoneNumberOptions.SmsFallbackMethod = phoneNumber.SmsFallbackMethod;

          var result = twilioclient.UpdateIncomingPhoneNumber(phoneNumber.Sid, PhoneNumberOptions);
          if (result.RestException != null)
          {
            APIExceptions.Add(result.RestException);
          }

        }

        var ExceptionMessages = string.Empty;
        if (APIExceptions.Count > 0)
        {
          foreach (var APIException in APIExceptions)
          {
            ExceptionMessages = APIException.Message + Environment.NewLine + Environment.NewLine;
          }
          MessageBox.Show("API Exceptions:" + Environment.NewLine + ExceptionMessages, "Twilio Exceptions", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
          MessageBox.Show("All URL's have been updated successfully!", "Twilio Update Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);


      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
      finally
      {
        twilioclient = null;
        this.Cursor = Cursors.Default;
      }

    }

    private void btnGetDebugger_Click(object sender, EventArgs e)
    {

      if (ValidateTwilioNumberTabFields() == false)
      {
        this.tbcTwilioControl.SelectedTab = this.tpTwilioParameters;
        return;
      }

      try
      {

        this.Cursor = Cursors.AppStarting;

        this.DebuggerList.Clear();

        twilioclient = new Twilio.TwilioRestClient(this.txtAccountSID.Text, this.txtAuthToken.Text);

        int Count = 0;
        int.TryParse(this.nudRetrieveRecords.Value.ToString(), out Count);

        var result = twilioclient.ListNotifications(0, null, 0, Count);

        this.DebuggerList.DataSource = result.Notifications;

        if (result.Notifications.Count == 0)
          MessageBox.Show("No Notifications via the Twilio Debugger are present", "No Notifications", MessageBoxButtons.OK, MessageBoxIcon.Information);

      }
      catch (Exception ex)
      {
        ex.DisplayException();
      }
      finally
      {
        this.Cursor = Cursors.Default;
        twilioclient = null;
      }


    }

    private void btnClickToCall_Click(object sender, EventArgs e)
    {

      if (lstPhoneNumbers.SelectedItem == null)
      {
        MessageBox.Show("Please select a Twilio number from the above list", "Twilio Number Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
      }

      if (string.IsNullOrWhiteSpace(this.txtDevelopersPhone.Text))
      {
        MessageBox.Show("Developers phone number cannot be blank.", "Developer Number Blank", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
      }

      if (IsServiceBusListening)
      {
        MessageBox.Show("Click to call is enabled only if Twiddler is listening via the service bus", "Twiddler is not listening", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
      }

      
      Twilio.IncomingPhoneNumber selectedTwilioNumber = this.lstPhoneNumbers.SelectedItem as Twilio.IncomingPhoneNumber;
      string URLStartsWith = string.Empty;
      if (selectedTwilioNumber.VoiceUrl.ToLower().StartsWith(@"https://"))
        URLStartsWith = @"https://";

      if (selectedTwilioNumber.VoiceUrl.ToLower().StartsWith(@"http://"))
        URLStartsWith = @"http://";

      if (string.IsNullOrWhiteSpace(URLStartsWith))
      {
        MessageBox.Show("Voice URL does not start with https, or http.  Cannot determine the Voice URL to be used");
        return;
      }
        
      bool ProceedwithCall = false;
      URLStartsWith = URLStartsWith + Utilities.ServiceFormBridge.NameSpaceName;

      if (selectedTwilioNumber.VoiceUrl.StartsWith(URLStartsWith) == false)
      {
        var WishToProceed = MessageBox.Show("Voice URL does not start with the Service Bus Namespace and hence, will not return to Twiddler.  Do you wish to proceed with this call?", "Twiddler will not intercept this call", MessageBoxButtons.YesNo,MessageBoxIcon.Question);
        if (WishToProceed == DialogResult.Yes)
          ProceedwithCall = true;
      }
      else
        ProceedwithCall = true;

      if (ProceedwithCall == false)
        return;

      Utilities.ServiceFormBridge.TwilioPhoneNumberBeingCalled = selectedTwilioNumber.PhoneNumber.LastXDigits(10);
      Utilities.ServiceFormBridge.DevelopersPhoneNumber = this.txtDevelopersPhone.Text.LastXDigits(10);

      //twilioclient with credentials - number must also be verified
      var client = new Twilio.TwilioRestClient(this.txtAccountSID.Text, this.txtAuthToken.Text);

      var calloptions = new Twilio.CallOptions();
      calloptions.From = selectedTwilioNumber.PhoneNumber;
      calloptions.To = this.txtDevelopersPhone.Text;

      //call from twiddler will be redirected to the URL of the phone number
      calloptions.Url = selectedTwilioNumber.VoiceUrl;
      calloptions.Method = selectedTwilioNumber.VoiceMethod;

      if (string.IsNullOrWhiteSpace(selectedTwilioNumber.StatusCallback))
      {
        calloptions.StatusCallback = selectedTwilioNumber.StatusCallback;
        calloptions.StatusCallbackMethod = selectedTwilioNumber.StatusCallbackMethod;
      }

      if (string.IsNullOrWhiteSpace(selectedTwilioNumber.VoiceFallbackUrl))
      {
        calloptions.FallbackUrl = selectedTwilioNumber.VoiceFallbackUrl;
        calloptions.FallbackMethod = selectedTwilioNumber.VoiceFallbackMethod;
      }

      var call = client.InitiateOutboundCall(calloptions);

    }

    public bool IsServiceBusListening
      {
        get
        {
          return this.lblStatusMessage.BackColor == Color.Green;
        }
      }


  }

}
