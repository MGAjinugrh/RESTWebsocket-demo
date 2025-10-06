using System.ComponentModel;

namespace Common;

public enum BookWsEvent
{
    [Description("NewEntry")]
    Add,
    [Description("Availability")]
    Available,
    [Description("Availability")]
    Unavailable,
    [Description("Removed")]
    Remove,
}
public enum BookStatus
{
    [Description("Available")]
    Available,
    [Description("Unavailable")]
    NotAvailable
}

public enum ReturnStatus
{
    Failed = 0,
    Success = 1,
    Unauthorized = 2,
    NotFound = 3,
    Duplicate = 4,
    BadRequest = 5,
    Created = 6,
    SuccessNoContent = 7
}
