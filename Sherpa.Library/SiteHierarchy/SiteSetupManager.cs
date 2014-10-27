﻿using System;
using System.Linq;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.SiteHierarchy
{
    public class SiteSetupManager : ISiteSetupManager
    {
        private readonly ShWeb _configurationWeb;
        private ClientContext ClientContext { get; set; }
        private FeatureManager FeatureManager { get; set; }
        private QuicklaunchManager QuicklaunchManager { get; set; }
        private PropertyManager PropertyManager { get; set; }
        private ListManager ListManager { get; set; }

        public SiteSetupManager(ClientContext clientContext, ShWeb configurationWeb)
        {
            _configurationWeb = configurationWeb;
            ClientContext = clientContext;

            FeatureManager = new FeatureManager();
            QuicklaunchManager = new QuicklaunchManager();
            PropertyManager = new PropertyManager();
            ListManager = new ListManager();
        }
        public void SetupSites()
        {
            EnsureAndConfigureWebAndActivateFeatures(ClientContext, null, _configurationWeb);
        }

        /// <summary>
        /// Assumptions:
        /// 1. The order of webs and subwebs in the config file follows the structure of SharePoint sites
        /// 2. No config element is present without their parent web already being defined in the config file, except the root web
        /// </summary>
        private void EnsureAndConfigureWebAndActivateFeatures(ClientContext context, Web parentWeb, ShWeb configWeb)
        {
            var webToConfigure = EnsureWeb(context, parentWeb, configWeb);

            SetWelcomePageUrlIfConfigured(context, webToConfigure, configWeb);
            FeatureManager.ActivateFeatures(context, webToConfigure, configWeb.SiteFeatures, configWeb.WebFeatures);
            ListManager.CreateLists(context, webToConfigure, configWeb.Lists);
            QuicklaunchManager.CreateQuicklaunchNodes(context, webToConfigure, configWeb.Quicklaunch);
            PropertyManager.SetProperties(context, webToConfigure, configWeb.Properties);

            foreach (ShWeb subWeb in configWeb.Webs)
            {
                EnsureAndConfigureWebAndActivateFeatures(context, webToConfigure, subWeb);
            }
        }

        private void SetWelcomePageUrlIfConfigured(ClientContext context, Web webToConfigure, ShWeb configWeb)
        {
            if (!string.IsNullOrEmpty(configWeb.WelcomePageUrl))
            {
                webToConfigure.RootFolder.WelcomePage = configWeb.WelcomePageUrl;
                context.Load(webToConfigure.RootFolder);
                context.ExecuteQuery();
            }
        }

        private Web EnsureWeb(ClientContext context, Web parentWeb, ShWeb configWeb)
        {
            Web webToConfigure;
            if (parentWeb == null)
            {
                //We assume that the root web always exists
                webToConfigure = context.Site.RootWeb;
            }
            else
            {
                webToConfigure = GetSubWeb(context, parentWeb, configWeb.Url);

                if (webToConfigure == null)
                {
                    Console.WriteLine("Creating web " + configWeb.Url);
                    webToConfigure = parentWeb.Webs.Add(GetWebCreationInformationFromConfig(configWeb));
                }
            }
            context.Load(webToConfigure, w => w.Url);
            context.ExecuteQuery();

            return webToConfigure;
        }

        private Web GetSubWeb(ClientContext context, Web parentWeb, string webUrl)
        {
            context.Load(parentWeb, w => w.Url, w => w.Webs);
            context.ExecuteQuery();

            var absoluteUrlToCheck = parentWeb.Url.TrimEnd('/') + '/' + webUrl;
            // use a simple linq query to get any sub webs with the URL we want to check
            return (from w in parentWeb.Webs where w.Url == absoluteUrlToCheck select w).SingleOrDefault();
        }

        /// <summary>
        /// Will only activate site collection features or rootweb's web features
        /// </summary>
        public void ActivateContentTypeDependencyFeatures()
        {
            FeatureManager.ActivateFeatures(ClientContext, ClientContext.Web, _configurationWeb.SiteFeatures, _configurationWeb.WebFeatures, true);
        }

        private WebCreationInformation GetWebCreationInformationFromConfig(ShWeb configWeb)
        {
            return new WebCreationInformation
                {
                    Title = configWeb.Name,
                    Description = configWeb.Description,
                    Language = configWeb.Language,
                    Url = configWeb.Url,
                    UseSamePermissionsAsParentSite = true,
                    WebTemplate = configWeb.Template
                };
        }

        public static void DeleteSites(ClientContext clientContext)
        {
            clientContext.Load(clientContext.Site.RootWeb.Webs);
            clientContext.ExecuteQuery();

            foreach (var web in clientContext.Site.RootWeb.Webs)
            {
                DeleteWeb(clientContext, web);
            }
        }

        private static void DeleteWeb(ClientContext clientContext, Web web)
        {
            clientContext.Load(web.Webs);
            clientContext.ExecuteQuery();

            foreach (Web subWeb in web.Webs)
            {
                DeleteWeb(clientContext, subWeb);
            }
            web.DeleteObject();
            clientContext.ExecuteQuery();
        }
    }
}
