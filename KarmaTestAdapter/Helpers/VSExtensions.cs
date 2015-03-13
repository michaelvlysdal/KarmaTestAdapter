﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace KarmaTestAdapter.Helpers
{
    public static class VSExtensions
    {
        private static IVsSolution GetSolution(this IServiceProvider serviceProvider)
        {
            return (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
        }

        public static IEnumerable<IVsProject> GetLoadedProjects(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetSolution().GetLoadedProjects();
        }

        public static string GetSolutionDirectory(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetSolution().GetSolutionDirectory();
        }

        public static IEnumerable<IVsProject> GetLoadedProjects(this IVsSolution solution)
        {
            return solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();
        }

        public static string GetProjectName(this IVsProject project)
        {
            object nameObj = null;
            var hr = ((IVsHierarchy)project).GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out nameObj);
            return nameObj as string;
        }

        public static IEnumerable<IVsHierarchy> EnumerateLoadedProjects(this IVsSolution solution, __VSENUMPROJFLAGS enumFlags)
        {
            var prjType = Guid.Empty;
            IEnumHierarchies ppHier;

            var hr = solution.GetProjectEnum((uint)enumFlags, ref prjType, out ppHier);
            if (ErrorHandler.Succeeded(hr) && ppHier != null)
            {
                uint fetched = 0;
                var hierarchies = new IVsHierarchy[1];
                while (ppHier.Next(1, hierarchies, out fetched) == VSConstants.S_OK)
                {
                    yield return hierarchies[0];
                }
            }
        }

        public static string GetSolutionDirectory(this IVsSolution solution)
        {
            string solutionDir;
            string solutionFile;
            string userOpsFile;
            if (solution.GetSolutionInfo(out solutionDir, out solutionFile, out userOpsFile) == VSConstants.S_OK)
            {
                return solutionDir;
            }
            return null;
        }

        public static IEnumerable<string> GetProjectItems(this IVsProject project)
        {
            // Each item in VS OM is IVSHierarchy. 
            return GetProjectItems((IVsHierarchy)project, VSConstants.VSITEMID_ROOT);
        }

        public static IEnumerable<string> GetProjectItems(IVsHierarchy project, uint itemId)
        {
            object pVar = GetPropertyValue((int)__VSHPROPID.VSHPROPID_FirstChild, itemId, project);

            uint childId = GetItemId(pVar);
            while (childId != VSConstants.VSITEMID_NIL)
            {
                string childPath = GetCanonicalName(childId, project);
                yield return childPath;

                foreach (var childNodePath in GetProjectItems(project, childId)) yield return childNodePath;

                pVar = GetPropertyValue((int)__VSHPROPID.VSHPROPID_NextSibling, childId, project);
                childId = GetItemId(pVar);
            }
        }

        public static IEnumerable<string> GetSources(this IVsProject project)
        {
            return project
                .GetProjectItems()
                .Where(f => PathUtils.IsSettingsFile(f) || PathUtils.IsKarmaConfigFile(f))
                .Where(f => File.Exists(f));
        }

        public static bool HasFile(this IVsProject project, string file)
        {
            return project
                .GetProjectItems()
                .Where(f => PathUtils.PathsEqual(f, file))
                .Where(f => File.Exists(f))
                .Any();
        }

        public static uint GetItemId(object pvar)
        {
            if (pvar == null) return VSConstants.VSITEMID_NIL;
            if (pvar is int) return (uint)(int)pvar;
            if (pvar is uint) return (uint)pvar;
            if (pvar is short) return (uint)(short)pvar;
            if (pvar is ushort) return (uint)(ushort)pvar;
            if (pvar is long) return (uint)(long)pvar;
            return VSConstants.VSITEMID_NIL;
        }

        public static object GetPropertyValue(int propid, uint itemId, IVsHierarchy vsHierarchy)
        {
            if (itemId == VSConstants.VSITEMID_NIL)
            {
                return null;
            }

            try
            {
                object o;
                ErrorHandler.ThrowOnFailure(vsHierarchy.GetProperty(itemId, propid, out o));

                return o;
            }
            catch (System.NotImplementedException)
            {
                return null;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                return null;
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        public static string GetCanonicalName(uint itemId, IVsHierarchy hierarchy)
        {
            string strRet = string.Empty;
            int hr = hierarchy.GetCanonicalName(itemId, out strRet);

            if (hr == VSConstants.E_NOTIMPL)
            {
                // Special case E_NOTIMLP to avoid perf hit to throw an exception.
                return string.Empty;
            }
            else
            {
                try
                {
                    ErrorHandler.ThrowOnFailure(hr);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    strRet = string.Empty;
                }

                // This could be in the case of S_OK, S_FALSE, etc.
                return strRet;
            }
        }
    }
}