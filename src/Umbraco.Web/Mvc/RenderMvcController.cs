using System;
using System.Web.Mvc;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web.Models;
using Umbraco.Web.Routing;

namespace Umbraco.Web.Mvc
{

    /// <summary>
    /// The default controller to render front-end requests
    /// </summary>
    [PreRenderViewActionFilter]    
    public class RenderMvcController : UmbracoController, IRenderMvcController
	{

		public RenderMvcController()
            : base()
		{
			ActionInvoker = new RenderActionInvoker();
		}

        public RenderMvcController(UmbracoContext umbracoContext, UmbracoHelper umbracoHelper)
            : base(umbracoContext, umbracoHelper)
        {
        }

        public RenderMvcController(UmbracoContext umbracoContext)
            : base(umbracoContext)
        {

        }

		private PublishedContentRequest _publishedContentRequest;

        /// <summary>
        /// Returns the current UmbracoContext
        /// </summary>
        public override UmbracoContext UmbracoContext
        {
            get { return PublishedContentRequest.RoutingContext.UmbracoContext; }
        }

        /// <summary>
        /// Returns the Current published content item for rendering the content
        /// </summary>
	    protected IPublishedContent CurrentPage
	    {
	        get { return PublishedContentRequest.PublishedContent; }
	    }

		/// <summary>
		/// Returns the current PublishedContentRequest
		/// </summary>
        protected internal virtual PublishedContentRequest PublishedContentRequest
		{
			get
			{
				if (_publishedContentRequest != null)
					return _publishedContentRequest;
                if (RouteData.DataTokens.ContainsKey(Core.Constants.Web.PublishedDocumentRequestDataToken) == false)
				{
					throw new InvalidOperationException("DataTokens must contain an 'umbraco-doc-request' key with a PublishedContentRequest object");
				}
                _publishedContentRequest = (PublishedContentRequest)RouteData.DataTokens[Core.Constants.Web.PublishedDocumentRequestDataToken];
				return _publishedContentRequest;
			}
		}

		/// <summary>
		/// Checks to make sure the physical view file exists on disk
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		protected bool EnsurePhsyicalViewExists(string template)
		{
            var result = ViewEngines.Engines.FindView(ControllerContext, template, null);
            if (result.View == null)
            {
                LogHelper.Warn<RenderMvcController>("No physical template file was found for template " + template);
                return false;
            }
            return true;
		}

		/// <summary>
		/// Returns an ActionResult based on the template name found in the route values and the given model.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="model"></param>
		/// <returns></returns>
		/// <remarks>
		/// If the template found in the route values doesn't physically exist, then an empty ContentResult will be returned.
		/// </remarks>
		protected ActionResult CurrentTemplate<T>(T model)
		{
			var template = ControllerContext.RouteData.Values["action"].ToString();
            if (EnsurePhsyicalViewExists(template) == false)
                throw new Exception("No physical template file was found for template " + template);
			return View(template, model);
		}

        public delegate ActionResult IndexActionEventHandler(RenderModel model, System.Web.HttpContextBase context);
        public static event IndexActionEventHandler OnIndexAction;

		/// <summary>
		/// The default action to render the front-end view
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
        [RenderIndexActionSelector]
		public virtual ActionResult Index(RenderModel model)
		{
            if (OnIndexAction != null)
            {
                var result = OnIndexAction(model, HttpContext);
                if (result != null)
                    return result;
            }

			return CurrentTemplate(model);
		}
	}
}