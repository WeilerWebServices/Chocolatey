﻿using System;
using System.Threading.Tasks;
#if VS14
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Designers;
#endif
using Microsoft.VisualStudio.Shell.Interop;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace NuGet.VisualStudio14
{
    public static class ProjectHelper
    {
#if VS14
        public static async void DoWorkInWriterLock(EnvDTE.Project project, IVsHierarchy hierarchy, Action<MsBuildProject> action)
        {
            await DoWorkInWriterLock((IVsProject)hierarchy, action);
            project.Save();
        }

        private static async Task DoWorkInWriterLock(IVsProject project, Action<MsBuildProject> action)
        {
            UnconfiguredProject unconfiguredProject = GetUnconfiguredProject(project);
            if (unconfiguredProject != null)
            {
                var service = unconfiguredProject.ProjectService.Services.ProjectLockService;
                if (service != null)
                {
                    using (ProjectWriteLockReleaser x = await service.WriteLockAsync())
                    {
                        await x.CheckoutAsync(unconfiguredProject.FullPath);
                        ConfiguredProject configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync();
                        MsBuildProject buildProject = await x.GetProjectAsync(configuredProject);

                        if (buildProject != null)
                        {
                            action(buildProject);
                        }

                        await x.ReleaseAsync();
                    }

                    await unconfiguredProject.ProjectService.Services.ThreadingPolicy.SwitchToUIThread();
                }
            }
        }

        private static UnconfiguredProject GetUnconfiguredProject(IVsProject project)
        {
            IVsBrowseObjectContext context = project as IVsBrowseObjectContext;
            if (context == null)
            {
                IVsHierarchy hierarchy = project as IVsHierarchy;
                if (hierarchy != null)
                {
                    object extObject;
                    if (ErrorHandler.Succeeded(hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out extObject)))
                    {
                        EnvDTE.Project dteProject = extObject as EnvDTE.Project;
                        if (dteProject != null)
                        {
                            context = dteProject.Object as IVsBrowseObjectContext;
                        }
                    }
                }
            }

            return context != null ? context.UnconfiguredProject : null;
        }
#else
        public static void DoWorkInWriterLock(EnvDTE.Project project, IVsHierarchy hierarchy, Action<MsBuildProject> action)
        {
        }
#endif
    }
}