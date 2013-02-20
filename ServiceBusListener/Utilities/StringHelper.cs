using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceBusListener.Utilities
{
  public static class StringHelper
  {
    
    public static string LastXDigits(this string pPhoneNumber, int pLength)
    { 
      if (pLength < 0)
        throw new ArgumentOutOfRangeException("Length must be greater than zero");

      string WorkingString = pPhoneNumber.Trim();
      if (string.IsNullOrWhiteSpace(WorkingString))
        return string.Empty;

      if (pPhoneNumber.Contains("+"))
        WorkingString = pPhoneNumber.Replace("+","");

      if (pPhoneNumber.Contains("("))
        WorkingString = WorkingString.Replace("(", "");

      if (pPhoneNumber.Contains(")"))
        WorkingString = WorkingString.Replace(")", "");

      if (pPhoneNumber.Contains(" "))
        WorkingString = WorkingString.Replace(" ", "");

      WorkingString = WorkingString.PadLeft(pLength);
      int StartingPosition = WorkingString.Length -pLength;
      WorkingString = WorkingString.Substring(StartingPosition, pLength);

      return WorkingString;
  
    }
  }
}
