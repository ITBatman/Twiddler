using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiddler;
using System.Windows.Forms;
using ServiceBusListener;
using System.IO;
using System.Reflection;
using util = ServiceBusListener.Utilities;
using System.Diagnostics;
using ServiceBusListener.Utilities;


namespace ServiceBusListener
{
  public class TwiddlerExtension : IAutoTamper
  {
    TabPage oTwiddlerPage = null;
    TwiddlerControl oView = null;
    System.Windows.Forms.Label lblLoading;

    public void OnLoad()
    {

      oTwiddlerPage = new TabPage("Twiddler");
      oTwiddlerPage.Name = "StartUp";

      //Read an internal Resource File to display the Twiddler Logo
      Stream customIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServiceBusListener.Resources.Twilio_57.png");
      FiddlerApplication.UI.imglSessionIcons.Images.Add("Twilio", System.Drawing.Image.FromStream(customIcon));
      oTwiddlerPage.ImageIndex = FiddlerApplication.UI.imglSessionIcons.Images.IndexOfKey("Twilio");

      this.lblLoading = new System.Windows.Forms.Label();
      this.lblLoading.Text = "Loading...";
      oTwiddlerPage.Controls.Add(lblLoading);
      FiddlerApplication.UI.tabsViews.TabPages.Add(oTwiddlerPage);
      
      //subscribe to the event that fires when a tab page has been selected
      //and wait for Twiddler to be selected
      TabControlEventHandler evtTCEH = null;
      evtTCEH = delegate(object s, TabControlEventArgs e)
      {
        if (e.TabPage == oTwiddlerPage && e.TabPage.Name == "StartUp")
        {
          
          var Twiddler = new TwiddlerControl();
          e.TabPage.Controls.Clear();
          e.TabPage.Controls.Add(Twiddler);
          e.TabPage.Name = "Twiddler";
          Twiddler.Refresh();

          // Remove the unneeded event handler.
          FiddlerApplication.UI.tabsViews.Selected -= evtTCEH;
        }
      };

      // Subscribe to tab-change events
      FiddlerApplication.UI.tabsViews.Selected += evtTCEH;

    }



    public void AutoTamperRequestAfter(Session oSession)
    {

    }

    public void AutoTamperRequestBefore(Session oSession)
    {

      
      try
      {

        //if DestinationWebSite is null, then we have not yet been setup correctly
        if (string.IsNullOrWhiteSpace(util.ServiceFormBridge.DestinationWebSite))
          return;

        //Twiddler uses ipv4.fiddler, which gets converted to the local ip of 127.0.0.1, so we have to do it here to match the URL
        var destinationAddress = util.ServiceFormBridge.DestinationWebSite.Replace("ipv4.fiddler", "127.0.0.1");

        //testing to see if the destination url is the same url as the destination we set in Twiddler
        if (string.IsNullOrWhiteSpace(destinationAddress) == false && oSession.fullUrl.StartsWith(destinationAddress))
        {

          var requestbody = oSession.GetRequestBodyAsString();

          //if we use the Twiddler functionality - the call was initiated with the outbound API
          if (requestbody.Contains("outbound-api") == false)
            return;

          string Last10DigitsOfDevelopersPhoneNumber = util.ServiceFormBridge.DevelopersPhoneNumber.LastXDigits(10);
          string Last10DigitsofTwiliosPhoneNumber = util.ServiceFormBridge.TwilioPhoneNumberBeingCalled.LastXDigits(10);

          //last check to see if we initiated this call to Twilio before we flip the phone numbers
          if (requestbody.Contains(Last10DigitsOfDevelopersPhoneNumber))
          {
            var Altered = requestbody.Replace(Last10DigitsOfDevelopersPhoneNumber, "9999999999");
            Altered = Altered.Replace(Last10DigitsofTwiliosPhoneNumber, Last10DigitsOfDevelopersPhoneNumber);
            Altered = Altered.Replace("9999999999", Last10DigitsofTwiliosPhoneNumber);

            byte[] byteArray = Encoding.UTF8.GetBytes(Altered);
            oSession.RequestBody = byteArray;
            oSession["ui-color"] = "purple"; //indicates an altered Request body
          }
        }
      }
      catch (Exception ex)
      {
        util.ExceptionHelper.WriteMessageToLog(string.Concat("AutoTamperBeforeRequest", Environment.NewLine, ex.Message), EventLogEntryType.Error);
      }
    }

    public void AutoTamperResponseAfter(Session oSession)
    {

    }

    public void AutoTamperResponseBefore(Session oSession)
    {

    }

    public void OnBeforeReturningError(Session oSession)
    {

    }

    public void OnBeforeUnload()
    {

    }
  }
}
