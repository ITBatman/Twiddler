using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace ServiceBusListener.Router
{
  // The Router, and general concept of how to recieve from the SB and redirect was taken from
  // Tony Sneed Blog - which he documented here: http://blog.tonysneed.com/2012/04/24/roll-your-own-rest-ful-wcf-router/
  //
    [ServiceContract(Namespace = "urn:ServiceBusAPIRelay")]
    public interface IRoutingService
    {
        [WebInvoke(UriTemplate = "")]
        [OperationContract(AsyncPattern = true, Action = "*", ReplyAction = "*")]
        IAsyncResult BeginProcessRequest(Message requestMessage, AsyncCallback asyncCallback, object asyncState);

        Message EndProcessRequest(IAsyncResult asyncResult);
    }
}
