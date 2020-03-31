using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace Recurly
{
  public class Price : RecurlyEntity
  {
    internal Price(XmlReader reader)
    {
      CurrencyCode = reader.Name;
      Amount = reader.ReadElementContentAsDecimal();
    }

    public Price(string currencyCode, decimal amount)
    {
      CurrencyCode = currencyCode ?? throw new ArgumentNullException(nameof(currencyCode));
      Amount = amount;
    }

    public bool IsValid => CurrencyCode != null;

    public string CurrencyCode { get; set; }
    public decimal Amount { get; set; }

    internal override void ReadXml(XmlTextReader reader)
    {
      CurrencyCode = reader.Name;
      Amount = reader.ReadElementContentAsDecimal();
    }

    internal override void WriteXml(XmlTextWriter writer)
    {
      writer.WriteElementString(CurrencyCode, Amount.ToString());
    }
  }

  /// <summary>
  /// An item in Recurly.
  ///
  /// </summary>
  public class Item : RecurlyEntity
  {
    public string ItemCode { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string ExternalSku { get; set; }

    public string AccountingCode { get; set; }

    public string RevenueScheduleType { get; set; }

    public string State { get; private set; }

    public string TaxCode { get; set; }

    public bool TaxExempt { get; set; }

    public Price[] UnitAmountInCents { get; set; }

    public List<CustomField> CustomFields
    {
      get { return _customFields ?? (_customFields = new List<CustomField>()); }
      set { _customFields = value; }
    }

    private List<CustomField> _customFields;

    public DateTime? CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    internal const string UrlPrefix = "/items/";

    #region Constructors

    public Item()
    {
    }

    internal Item(XmlTextReader xmlReader)
    {
      ReadXml(xmlReader);
    }

    // internal Item(XmlTextReader xmlReader, string xmlName)
    // {
    //     ReadXml(xmlReader, xmlName);
    // }

    public Item(string itemCode, string name)
    {
      ItemCode = itemCode;
      Name = name;
    }

    #endregion

    /// <summary>
    /// Create a new Item in Recurly
    /// </summary>
    public void Create()
    {
      Client.Instance.PerformRequest(Client.HttpRequestMethod.Post,
          UrlPrefix,
          WriteXml,
          ReadXml);
    }

    /// <summary>
    /// Update an existing account in Recurly
    /// </summary>
    public void Update()
    {
      // PUT /items/<item_code>
      Client.Instance.PerformRequest(Client.HttpRequestMethod.Put,
          UrlPrefix + Uri.EscapeDataString(ItemCode),
          WriteXml);
    }

    public void Deactivate()
    {
      Client.Instance.PerformRequest(Client.HttpRequestMethod.Delete, UrlPrefix + Uri.EscapeDataString(ItemCode));
    }

    public void Reactivate()
    {
      Client.Instance.PerformRequest(Client.HttpRequestMethod.Put,
      UrlPrefix + Uri.EscapeDataString(ItemCode) + "/reactivate",
      ReadXml);
    }

    internal override void ReadXml(XmlTextReader reader)
    {
      while (reader.Read())
      {
        if (reader.Name == "item" && reader.NodeType == XmlNodeType.EndElement)
          break;

        if (reader.NodeType != XmlNodeType.Element) continue;

        switch (reader.Name)
        {
          case "item_code":
            ItemCode = reader.ReadElementContentAsString();
            break;

          case "name":
            Name = reader.ReadElementContentAsString();
            break;

          case "description":
            Description = reader.ReadElementContentAsString();
            break;

          case "unit_amount_in_cents":
            UnitAmountInCents = ReadPrices(reader);
            break;

          case "tax_code":
            TaxCode = reader.ReadElementContentAsString();
            break;

          case "tax_exempt":
            TaxExempt = reader.ReadElementContentAsBoolean();
            break;

          case "external_sku":
            ExternalSku = reader.ReadElementContentAsString();
            break;

          case "state":
            State = reader.ReadElementContentAsString();
            break;

          case "created_at":
            CreatedAt = reader.ReadElementContentAsString().AsDateTime();
            break;

          case "updated_at":
            UpdatedAt = reader.ReadElementContentAsString().AsDateTime();
            break;

          case "deleted_at":
            DeletedAt = reader.ReadElementContentAsString().AsDateTime();
            break;
        }
      }
    }

    private Price[] ReadPrices(XmlTextReader reader)
    {
      var list = new List<Price>();
      while (reader.Read())
      {
        if (reader.Name == "unit_amount_in_cents" && reader.NodeType == XmlNodeType.EndElement)
          break;

        if (reader.NodeType == XmlNodeType.Element)
        {
          var price = new Price(reader);
          list.Add(price);
        }
      }
      return list.ToArray();
    }

    internal override void WriteXml(XmlTextWriter xmlWriter)
    {
      xmlWriter.WriteStartElement("item");

      xmlWriter.WriteElementString("item_code", ItemCode);
      xmlWriter.WriteElementString("name", Name);
      xmlWriter.WriteStringIfValid("description", Description);
      xmlWriter.WriteStringIfValid("external_sku", ExternalSku);
      xmlWriter.WriteStringIfValid("accounting_code", AccountingCode);
      xmlWriter.WriteStringIfValid("revenue_schedule_type", RevenueScheduleType);
      xmlWriter.WriteStringIfValid("state", State);
      xmlWriter.WriteIfCollectionHasAny("custom_fields", CustomFields);
      xmlWriter.WriteIfCollectionHasAny("unit_amount_in_cents", UnitAmountInCents);

      xmlWriter.WriteEndElement();
    }
  }

  public sealed class Items
  {
    internal const string UrlPrefix = "/items/";

    /// <summary>
    /// Retrieves a list of all active items
    /// </summary>
    /// <returns></returns>
    public static RecurlyList<Item> List()
    {
      return List(null);
    }

    public static RecurlyList<Item> List(FilterCriteria filter)
    {
      filter = filter == null ? FilterCriteria.Instance : filter;
      return new ItemList(Item.UrlPrefix + "?" + filter.ToNamedValueCollection().ToString());
    }

    public static Item Get(string itemCode)
    {
      if (string.IsNullOrWhiteSpace(itemCode))
      {
        return null;
      }

      var item = new Item();

      var statusCode = Client.Instance.PerformRequest(Client.HttpRequestMethod.Get,
        UrlPrefix + Uri.EscapeDataString(itemCode),
        item.ReadXml);

      return statusCode == HttpStatusCode.NotFound ? null : item;
    }

  }

}
