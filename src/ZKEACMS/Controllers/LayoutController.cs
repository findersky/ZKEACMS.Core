/* http://www.zkea.net/ 
 * Copyright (c) ZKEASOFT. All rights reserved. 
 * http://www.zkea.net/licenses */

using Easy;
using ZKEACMS.Common.ViewModels;
using Easy.Constant;
using Easy.Extend;
using Easy.Mvc.Attribute;
using Easy.Mvc.Authorize;
using Easy.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using ZKEACMS.Layout;
using ZKEACMS.Page;
using ZKEACMS.Widget;
using ZKEACMS.Zone;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ZKEACMS.Controllers
{
    [DefaultAuthorize(Policy = PermissionKeys.ViewLayout)]
    public class LayoutController : BasicController<LayoutEntity, string, ILayoutService>
    {
        private readonly IPageService _pageService;
        private readonly IZoneService _zoneService;
        private readonly IWidgetBasePartService _widgetService;
        private readonly IPageContext _pageContext;
        public LayoutController(ILayoutService service, 
            IPageService pageService, 
            IZoneService zoneService, 
            IWidgetBasePartService widgetService, 
            IPageContext pageContext)
            : base(service)
        {
            _pageService = pageService;
            _zoneService = zoneService;
            _widgetService = widgetService;
            _pageContext = pageContext;
        }


        public override IActionResult Index()
        {
            return View(Service.GetWithFull());
        }

        public IActionResult LayoutWidget(string layoutId)
        {
            ViewBag.LayoutId = layoutId;
            return View(Service.Get().ToList());
        }
        [HttpPost]
        public IActionResult LayoutZones(string ID)
        {
            var layout = Service.Get(ID);
            var viewModel = new LayoutZonesViewModel
            {
                Layout = layout,
                LayoutId = ID,
                Zones = _zoneService.GetByLayoutId(ID),
                Widgets = _widgetService.GetByLayoutId(ID),
                LayoutHtml = layout.Html
            };
            return View(viewModel);
        }

        public override IActionResult Create()
        {
            return View(new LayoutEntity { ImageUrl = LayoutEntity.DefaultThumbnial, ImageThumbUrl = LayoutEntity.DefaultThumbnial });
        }
        [HttpPost, DefaultAuthorize(Policy = PermissionKeys.ManageLayout)]
        public override IActionResult Create(LayoutEntity entity)
        {
            base.Create(entity);
            return RedirectToAction("Design", new { entity.ID });
        }

        public override IActionResult Edit(string ID)
        {
            ViewBag.CanDelete = _pageService.Count(m => m.LayoutId == ID) == 0;
            return base.Edit(ID);
        }

        [HttpPost, DefaultAuthorize(Policy = PermissionKeys.ManageLayout)]
        public override IActionResult Edit(LayoutEntity entity)
        {
            if (entity.ActionType.HasFlag(ActionType.Design))
            {
                return RedirectToAction("Design", new { entity.ID });
            }
            return base.Edit(entity);
        }
        [ResponseCache(NoStore = true)]
        public IActionResult Design(string ID, string PageId)
        {
            LayoutEntity layout = null;
            if (PageId.IsNotNullAndWhiteSpace())
            {
                var page = _pageService.Get(PageId);
                layout = Service.GetByPage(page);
                layout.Page = page;
            }
            else if (ID.IsNotNullAndWhiteSpace())
            {
                layout = Service.Get(ID);
            }
            if (layout == null)
            {
                return NotFound();
            }
            _pageContext.Meta.Add(new MetaTag(new { http_equiv = "cache-control", content = "no-cache" }));
            _pageContext.Meta.Add(new MetaTag(new { http_equiv = "cache-control", content = "no-store" }));
            _pageContext.Meta.Add(new MetaTag(new { http_equiv = "expires", content = "-1" }));
            _pageContext.Meta.Add(new MetaTag(new { http_equiv = "pragma", content = "no-cache" }));
            if (layout.Style.IsNotNullAndWhiteSpace())
            {
                _pageContext.StyleSheets.Add(layout.Style);
            }
            if (layout.Script.IsNotNullAndWhiteSpace())
            {
                _pageContext.FooterScripts.Add(layout.Script);
            }
            return View(layout ?? new LayoutEntity());
        }
        [DefaultAuthorize(Policy = PermissionKeys.ManageLayout)]
        public IActionResult SaveLayout(string[] html, LayoutEntity layoutEntity, ZoneCollection zones)
        {
            layoutEntity.Html = Zone.Helper.GenerateHtml(html, zones);
            layoutEntity.Zones = zones;
            Service.UpdateDesign(layoutEntity);
            if (layoutEntity.Page != null)
            {
                return RedirectToAction("Design", "Page", new { layoutEntity.Page.ID });
            }
            return RedirectToAction("Index");
        }
        public IActionResult SelectZone(string layoutId, string pageId, string zoneId)
        {
            LayoutEntity layou = null;
            if (layoutId.IsNotNullAndWhiteSpace())
            {
                layou = Service.Get(layoutId);
            }
            else if (pageId.IsNotNullAndWhiteSpace())
            {
                layou = Service.Get(_pageService.Get(pageId).LayoutId);
            }
            ViewBag.ZoneId = zoneId;
            return View(layou);
        }
    }
}
