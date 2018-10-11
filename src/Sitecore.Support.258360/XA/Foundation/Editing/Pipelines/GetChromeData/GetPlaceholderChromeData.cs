namespace Sitecore.Support.XA.Foundation.Editing.Pipelines.GetChromeData
{
  using Microsoft.Extensions.DependencyInjection;
  using Sitecore;
  using Sitecore.Configuration;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.DependencyInjection;
  using Sitecore.Diagnostics;
  using Sitecore.Layouts;
  using Sitecore.Pipelines;
  using Sitecore.Pipelines.GetChromeData;
  using Sitecore.Pipelines.GetPlaceholderRenderings;
  using Sitecore.StringExtensions;
  using Sitecore.Web.UI.PageModes;
  using Sitecore.XA.Foundation.Abstractions;
  using Sitecore.XA.Foundation.PlaceholderSettings.Services;
  using Sitecore.XA.Foundation.Presentation;
  using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Web;

  public class GetPlaceholderChromeData : Sitecore.XA.Foundation.Editing.Pipelines.GetChromeData.GetPlaceholderChromeData
  {
    public GetPlaceholderChromeData(ILayoutsPageContext layoutsPageContext)
        : base(layoutsPageContext)
    {
    }

    public override void Process(GetChromeDataArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.IsNotNull(args.ChromeData, "Chrome Data");
      if ("placeholder".Equals(args.ChromeType, StringComparison.OrdinalIgnoreCase))
      {
        string placeholderKey = args.CustomData["placeHolderKey"] as string;
        Assert.ArgumentNotNull(placeholderKey, "CustomData[\"{0}\"]".FormatWith("placeHolderKey"));
        args.ChromeData.DisplayName = StringUtil.GetLastPart(placeholderKey, '/', placeholderKey);
        Item designItem = ServiceLocator.ServiceProvider.GetService<IPresentationContext>().DesignItem;
        if (!(args.Item.InheritsFrom(Templates.PartialDesign.ID) ? GetRenderingsFromSnippet(args.Item, Context.Device, false) : GetInjectionsFromItem(designItem, Context.Device)).Any(delegate (RenderingReference r)
        {
          if (!r.Placeholder.Equals(placeholderKey))
          {
            return r.Placeholder.Equals("/" + placeholderKey);
          }
          return true;
        }))
        {
          List<WebEditButton> buttons = GetButtons("/sitecore/content/Applications/WebEdit/Default Placeholder Buttons");
          AddButtonsToChromeData(buttons, args);
        }
        Item item = null;
        bool hasPlaceholderSettings = false;
        if (args.Item != null)
        {
          string layout = ChromeContext.GetLayout(args.Item);
          GetPlaceholderRenderingsArgs getPlaceholderRenderingsArgs = new GetPlaceholderRenderingsArgs(placeholderKey, layout, args.Item.Database)
          {
            OmitNonEditableRenderings = true
          };
          CorePipeline.Run("getPlaceholderRenderings", getPlaceholderRenderingsArgs);
          hasPlaceholderSettings = getPlaceholderRenderingsArgs.HasPlaceholderSettings;
          var allowedRenderings = getPlaceholderRenderingsArgs.PlaceholderRenderings?.Select(i => i.ID.ToShortID().ToString()).ToList() ?? new List<string>();
          if (!args.ChromeData.Custom.ContainsKey(AllowedRenderingsKey))
          {
            args.ChromeData.Custom.Add(AllowedRenderingsKey, allowedRenderings);
          }
          IList<Item> sxaPlaceholderItems = LayoutsPageContext.GetSxaPlaceholderItems(layout, placeholderKey, args.Item, Context.Device.ID);
          if (sxaPlaceholderItems.Any())
          {
            item = sxaPlaceholderItems.FirstOrDefault();
          }
          else
          {
            #region Modified code
            Item sxaPlaceholderItem = LayoutsPageContext.GetSxaPlaceholderItem(getPlaceholderRenderingsArgs.PlaceholderKey, args.Item);
            #endregion
            item = ((sxaPlaceholderItem == null) ? LayoutsPageContext.GetPlaceholderItem(getPlaceholderRenderingsArgs.PlaceholderKey, args.Item.Database, layout) : sxaPlaceholderItem);
          }
          if (!getPlaceholderRenderingsArgs.PlaceholderKey.EndsWith("*", StringComparison.Ordinal) || sxaPlaceholderItems.Any())
          {
            args.ChromeData.DisplayName = ((item == null) ? StringUtil.GetLastPart(getPlaceholderRenderingsArgs.PlaceholderKey, '/', getPlaceholderRenderingsArgs.PlaceholderKey) : HttpUtility.HtmlEncode(item.DisplayName));
          }
        }
        else if (!args.ChromeData.Custom.ContainsKey("allowedRenderings"))
        {
          args.ChromeData.Custom.Add("allowedRenderings", new List<string>());
        }
        SetEditableChromeDataItem(args, item, hasPlaceholderSettings);
      }
    }
  }
}