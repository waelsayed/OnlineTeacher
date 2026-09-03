namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// Supported external payment methods used to credit a student wallet via a Transfer Request.
/// Currently limited to the documented Egyptian mobile-payment channels.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Vodafone Cash mobile wallet transfer.</summary>
    VodafoneCash = 0,

    /// <summary>InstaPay transfer.</summary>
    InstaPay = 1
}
