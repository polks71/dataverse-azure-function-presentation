#pragma warning disable CS1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataverseAzureFunctionsCommon.Dataverse.Model
{
	
	
	[System.Runtime.Serialization.DataContractAttribute()]
	public enum RjB_TypeOfAzureFunction
	{
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
		[OptionSetMetadataAttribute("EventGrid", 3)]
		Eventgrid = 911620003,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
		[OptionSetMetadataAttribute("HttpTriggered", 0)]
		Httptriggered = 911620000,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
		[OptionSetMetadataAttribute("ServiceBus", 1)]
		Servicebus = 911620001,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
		[OptionSetMetadataAttribute("TwoWay", 2)]
		Twoway = 911620002,
	}
}
#pragma warning restore CS1591
