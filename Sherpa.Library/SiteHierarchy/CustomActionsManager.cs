﻿using System;
using System.Reflection;
using Microsoft.SharePoint.Client;
using log4net;
using Sherpa.Library.SiteHierarchy.Model;
using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy
{
    public class CustomActionsManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void SetUpCustomActions(ClientContext context, List<ShCustomAction> customActions)
        {
            Log.Info("Adding custom actions");
            Site site = context.Site;
            context.Load(site);
            context.Load(site.UserCustomActions);
            context.ExecuteQuery();

            for (var i = site.UserCustomActions.Count - 1; i >= 0; i--)
            {
                var customAction = site.UserCustomActions[i];
                customAction.DeleteObject();
            }

            if (context.HasPendingRequest)
            {
                context.ExecuteQuery();
            }

            foreach (ShCustomAction customAction in customActions)
            {
                if (customAction.Location == null)
                {
                    Log.Error("You need to specify a location for your Custom Action. Ignoring " + customAction.ScriptSrc);
                    continue;
                }

                Log.DebugFormat("Adding custom action with src '{0}' at location '{1}'", customAction.ScriptSrc, customAction.Location);

                UserCustomAction userCustomAction = site.UserCustomActions.Add();
                userCustomAction.Location = customAction.Location;
                userCustomAction.Sequence = customAction.Sequence;
                userCustomAction.ScriptSrc = customAction.ScriptSrc;
                userCustomAction.ScriptBlock = customAction.ScriptBlock;

                userCustomAction.Description = customAction.Description;
                userCustomAction.RegistrationId = customAction.RegistrationId;
                userCustomAction.RegistrationType = customAction.RegistrationType;
                userCustomAction.Title = customAction.Title;
                userCustomAction.Name = customAction.Name;
                userCustomAction.ImageUrl = customAction.ImageUrl;
                userCustomAction.Group = customAction.Group;

                try
                {
                    userCustomAction.Update();
                    context.ExecuteQuery();
                    Log.Debug("Custom action successfully added.");
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                }
            }
        }
    }
}