using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ServiceBusListener
{
  
  public class StringBodyWriter : System.ServiceModel.Channels.BodyWriter
  {
    string body;
    public StringBodyWriter(string strData)
      : base(true)
    {
      body = strData;
    }
    protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
    {
      writer.WriteRaw(body);
    }
  } // class myBodyWriter
}