using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceBusListener.Utilities
{
  public static class ServiceFormBridge
  {

    /// <summary>
    /// Service bus Namespace Name
    /// </summary>
    public static string  NameSpaceName  {get;set;}

    /// <summary>
    /// Name of the EndPoint within the Service bus namespace name
    /// </summary>
    public static string EndPointName { get; set; }

    /// <summary>
    /// The URL that was used to send a traffic to the ServiceBus endpoint
    /// Used in case traffice arrives to endpoint, that is not targeted for the Twilio application
    /// </summary>
    public static string FullServiceBusNamespaceURL { get; set; }

    /// <summary>
    /// DestinationWebSite is web server (ipv4.fiddler), and the web site name under the default web site (Host Header Name)
    /// </summary>
    public static string DestinationWebSite { get; set; }

    /// <summary>
    /// Used to determine if the developer is phoning the twilio application
    /// if so, the From and To fields from Twilio are switched making it look like
    /// the call was from the developer to the application
    /// </summary>
    public static string DevelopersPhoneNumber { get; set; }

    /// <summary>
    /// Click-to-Call will set this field with the phone number that is selected
    /// Both the Developer Phone Number, and the TwilioNumber is required for the Click to call to work
    /// </summary>
    public static string TwilioPhoneNumberBeingCalled { get; set; }

    /// <summary>
    /// reference to the text box which displays the error messages
    /// </summary>
    internal static System.Windows.Forms.TextBox ErrorMessage { get; set; }
    
    /// <summary>
    /// Set the error message on the windows form while running as a service
    /// </summary>
    /// <param name="pErrorMessage"></param>
    public static void SetErrorMessage(string pErrorMessage)
    {
      if (ErrorMessage != null)
      {
        ErrorMessage.Text += pErrorMessage;
        ErrorMessage.Visible = true;
      }
    }
  }
}
