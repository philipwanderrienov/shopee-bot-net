using System;

namespace jitu_dashboard.Server.Models;

public class PaymentHistoryModel
{
    // Key column - keep non-nullable (should exist in every row)
    public string Trn { get; set; }

    public string? RelTrn { get; set; }
    public DateTime? BusDate { get; set; }
    public DateTime? SettleDate { get; set; }

    public string? DebPartiClient { get; set; }
    public string? AccDebPartiClient { get; set; }
    public string? CrPartiClient { get; set; }
    public string? AccCrPartiClient { get; set; }
    public string? FromMember { get; set; }
    public string? ToMember { get; set; }
    public string? DebAccount { get; set; }
    public string? DebAccountOriName { get; set; }

    public string? CrAccount { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? RemInfo { get; set; }
    public string? DetCharges { get; set; }
    public string? TxTypeId { get; set; }
    public string? TxCode { get; set; }
    public string? SendRecInfo { get; set; }

    public int? PriorityId { get; set; }

    public string? ChgComments { get; set; }
    public string? Editor { get; set; }
    public string? CurrencyToBuy { get; set; }
    public decimal? AmountToBuy { get; set; }
    public string? BatchRefId { get; set; }
    public DateTime? DateStamp { get; set; }
    public string? Source { get; set; }
    public string? CustomerChannel { get; set; }
    public string? BeneficiaryCategory { get; set; }
    public string? OldTrn { get; set; }
    public string? UpdUser { get; set; }
    public string? AppUser { get; set; }

    public DateTime? ElstTime { get; set; }
    public DateTime? LstTime { get; set; }
    public DateTime? RjtTime { get; set; }

    public string? Branch { get; set; }
    public string? Dept { get; set; }
    public string? IsAltered { get; set; }
}
