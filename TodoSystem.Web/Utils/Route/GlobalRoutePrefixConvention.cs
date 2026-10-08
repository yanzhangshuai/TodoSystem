using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace TodoSystem.Web.Utils.Route;

/// <summary>给所有控制器路由统一加上前缀，拼成 /api/v1/... 。</summary>
public class GlobalRoutePrefixConvention(IRouteTemplateProvider routeTemplateProvider) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _centralPrefix = new(routeTemplateProvider);

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var matchedSelectors = controller.Selectors
                .Where(x => x.AttributeRouteModel != null)
                .ToList();

            foreach (var selector in matchedSelectors)
            {
                selector.AttributeRouteModel =
                    AttributeRouteModel.CombineAttributeRouteModel(
                        _centralPrefix,
                        selector.AttributeRouteModel);
            }

            var unmatchedSelectors = controller.Selectors
                .Where(x => x.AttributeRouteModel == null)
                .ToList();

            foreach (var selector in unmatchedSelectors)
            {
                selector.AttributeRouteModel = _centralPrefix;
            }
        }
    }
}
