using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using ServiceBusListener.Utilities;
using System.Xml;
using System.IO;

// The Router, and general concept of how to recieve from the SB and redirect was taken from
// Tony Sneed Blog - which he documented here: http://blog.tonysneed.com/2012/04/24/roll-your-own-rest-ful-wcf-router/
//

namespace ServiceBusListener.Router
{
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
      AddressFilterMode = AddressFilterMode.Any, ValidateMustUnderstand = false)]
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class RoutingService : IRoutingService, IDisposable
  {
    private IRoutingService _client;

    /// <summary>
    /// when a message is received from the SB, it arrives here as simply a message - 
    /// </summary>
    /// <param name="requestMessage"></param>
    /// <param name="asyncCallback"></param>
    /// <param name="asyncState"></param>
    /// <returns></returns>
    public IAsyncResult BeginProcessRequest(Message requestMessage, AsyncCallback asyncCallback, object asyncState)
    {

#if DEBUG
      // Show incoming message
      //string message = OperationContext.Current.RequestContext.RequestMessage.ToString();
      Debug.WriteLine(string.Format("Request received at: {0}\n\tTo: {1}\n\tAction: {2}\n",
          DateTime.Now, requestMessage.Headers.To, requestMessage.Headers.Action));
#endif

      #region BadAttemptAndWhatILearned
      //this is now a SOAP message, and we need to get to the requestMessage body in order to inspect it

      //ServiceBus converts an HTTP POST into a SOAP Message, which is how I handle the message
      //in this section of code - it remains in the SOAP format, I only rewrite the URL and alter a host HEADER property
      // using Writemessage i was able to see the message, but it was binary encoded, and would need to decode that properly
      //to have the ability to change it.  Also, as I'm now reading the body of the message, I would also have to recreate it
      //as the body can only be read once.  

      //In short, it would be easier to manipulat the message after it has been converted back to a POST to the local web server
      //and alter the encoded variables at that point, rather than attempting to that here....
      
      //see if the developer started the phone call from twiddler - if so, the From and To numbers need to be switched.
      //i.e. if Twidder started the call, Twiddler instructed Twilio to call the Developer (TO=developer Phone Number)
      //what we want to simulate, is that developer was calling their own application (TO=Twilio Application)
      //hence we need to switch the numbers, as the app may use the From number to determine what Twilio number was called
      //requestMessage.GetBodyAttribute("TO",

      //MessageBuffer requestmessageDuplicate = requestMessage.CreateBufferedCopy(int.MaxValue);

      //MemoryStream ms = new MemoryStream();
      //requestmessageDuplicate.WriteMessage(ms);
      //ms.Flush();
      //ms.Position = 0;

      

      //var binaryData = new Byte[ms.Length];
      //ms.Read(binaryData, 0, (int)ms.Length);

      //System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
      //var str = enc.GetString(binaryData);


      //byte[] decodeData = Convert.FromBase64String(str);



      
      

      //XmlReader bodyXmlReader = requestMessage.GetReaderAtBodyContents();
      //string newXmlString = bodyXmlReader.ReadContentAsString();
      //if (newXmlString.Contains(Utilities.ServiceFormBridge.DevelopersPhoneNumber))
      //  System.Diagnostics.Debugger.Break();
      
      //var newBody = new StringBodyWriter(newXmlString);
      //Message replaceMessage = Message.CreateMessage(requestMessage.Version, null, newBody);

      #endregion

      IAsyncResult asyncResult = null;



      //if the full URI for the namespace does not match the one contructed in Twiddler, then pass it through - we have nothing to do with it!
      if (requestMessage.Headers.To.AbsoluteUri.Contains(Utilities.ServiceFormBridge.NameSpaceName) && requestMessage.Headers.To.AbsoluteUri.Contains(Utilities.ServiceFormBridge.EndPointName) == false)
        return asyncResult;

      //as the service bus will accept anything in terms of controllers and actions, we only need alter the DestinationAddress.Authority (host and port)
      var RewriteTheURL = requestMessage.Headers.To.AbsoluteUri.Replace(string.Format("http://{0}.servicebus.windows.net/{1}/", ServiceFormBridge.NameSpaceName, ServiceFormBridge.EndPointName), ServiceFormBridge.DestinationWebSite);

      Uri DestinationAddress = null;
      try
      {
        //DestinationAddress = new Uri(RewriteTheURL);
        DestinationAddress = new Uri(RewriteTheURL);
      }
      catch (Exception ex)
      {
        ServiceFormBridge.SetErrorMessage(string.Format("Rewritten URL: {0} is invalid." + Environment.NewLine + "Error: {1}", Utilities.ServiceFormBridge.DestinationWebSite, ex.Message));
        //if host is not listening, or the address is wrong, we get an exception here
        //we should perhaps test it before establishing the connection with SB?
        //fiddler ignores all errors and won't show the exception
        return asyncResult;
      }


      System.ServiceModel.ChannelFactory<IRoutingService> factory = null;
      try
      {
        // Create channel factory
        factory = new ChannelFactory<IRoutingService>(new WebHttpBinding(), DestinationAddress.AbsoluteUri);
      }
      catch (Exception ex)
      {
        ServiceFormBridge.SetErrorMessage(string.Format("Unable to open a channel using URL: {0}." + Environment.NewLine + "Error: {1}", DestinationAddress.AbsoluteUri, ex.Message));
        //if host is not listening, or the address is wrong, we get an exception here
        //we should perhaps test it before establishing the connection with SB?
        //fiddler ignores all errors and won't show the exception
        return asyncResult;
      }

      //make sure the Headers are correct
      WebHeaderCollection httpHeaders = WebOperationContext.Current.IncomingRequest.Headers;
      httpHeaders.Remove("Host");
      httpHeaders.Add("Host", DestinationAddress.Authority); //give it the new host that we are re-directing to

      httpHeaders.Remove("Connection");  //todo: not sure I need this, but without it there is an exception between Keep-Alive and Closed
      

      //this is not needed as I was able to get the controller to indicate the media type that is being returned
      //httpHeaders.Remove("Accept"); //want to make sure the twilio response is in XML - is there a better way to do this?
      //httpHeaders.Add("Accept", "application/xml");


      // Set factory and message address
      factory.Endpoint.Address = new EndpointAddress(DestinationAddress);
      requestMessage.Headers.To = DestinationAddress;

      // Create client channel
      try
      {
        _client = factory.CreateChannel();
      }
      catch (Exception ex)
      {
        ServiceFormBridge.SetErrorMessage(string.Format("Connection with URL: {0} had an issue." + Environment.NewLine + "Error: {1}", DestinationAddress.AbsoluteUri, ex.Message));
        //if host is not listening, or the address is wrong, we get an exception here
        //we should perhaps test it before establishing the connection with SB?
        //fiddler ignores all errors and won't show the exception
      }

      // Begin request, which will then wait for the reply async - iAsyncResult can be checked on for the status of the call - ??await keyword if we had 4.5 in fiddler??
      try
      {
        asyncResult = _client.BeginProcessRequest(requestMessage, asyncCallback, asyncState);
      }
      catch (Exception ex)
      {
        ServiceFormBridge.SetErrorMessage(string.Format("Post to the local web service failed using this URL:{0}" + Environment.NewLine + "Error:{1}",DestinationAddress.AbsoluteUri, ex.Message));
      }
      return asyncResult;
    }
    //*********************************************************************************************************
    public Message EndProcessRequest(IAsyncResult asyncResult)
    {

      Message messageResult = null;

      try
      {
        messageResult = _client.EndProcessRequest(asyncResult);
      }
      catch (Exception ex)
      {
        ServiceFormBridge.SetErrorMessage(string.Format("Returning result from the local service failed, see error" + Environment.NewLine + " Error: {0}", ex.Message));
        //if host is not listening, or the address is wrong, we get an exception here
        //we should perhaps test it before establishing the connection with SB?
        //fiddler ignores all errors and won't show the exception
      }


      return messageResult;
    }

    public void Dispose()
    {
      if (_client != null)
      {
        var channel = (IClientChannel)_client;
        if (channel.State != CommunicationState.Closed)
        {
          try
          {
            channel.Close();
          }
          catch
          {
            channel.Abort();
          }
        }
      }
    }
  }
}
