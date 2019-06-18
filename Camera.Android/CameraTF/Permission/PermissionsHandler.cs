using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Content;
using Android.Support.V4.Content;
using Android.Support.V4.App;

namespace CameraTF
{
    public static class PermissionsHandler
    {
        static TaskCompletionSource<bool> requestCompletion = null;

        public static Task PermissionRequestTask
        {
            get
            {
                return requestCompletion?.Task ?? Task.CompletedTask;
            }
        }

        public static bool NeedsPermissionRequest(Context context)
        {
            var permissionsToRequest = new List<string>();

            // Check and request any permissions
            foreach (var permission in MainActivity.RequiredPermissions)
            {
                if (IsPermissionInManifest(context, permission))
                {
                    if (!IsPermissionGranted(context, permission))
                        return true;
                }
            }

            return false;
        }

        public static Task<bool> RequestPermissionsAsync (Activity activity)
        {
            if (requestCompletion != null && !requestCompletion.Task.IsCompleted)
                return requestCompletion.Task;

            var permissionsToRequest = new List<string>();

            // Check and request any permissions
            foreach (var permission in MainActivity.RequiredPermissions)
            {
                if (IsPermissionInManifest(activity, permission))
                {
                    if (!IsPermissionGranted(activity, permission))
                        permissionsToRequest.Add(permission);
                }
            }

            if (permissionsToRequest.Any())
            {
                DoRequestPermissions(activity, permissionsToRequest.ToArray(), 101);
                requestCompletion = new TaskCompletionSource<bool>();

                return requestCompletion.Task;
            }

            return Task.FromResult<bool>(true);
        }

        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCompletion != null && !requestCompletion.Task.IsCompleted)
            {
                var success = true;

                foreach (var gr in grantResults)
                {
                    if (gr == Permission.Denied)
                    {
                        success = false;
                        break;
                    }
                }

                requestCompletion.TrySetResult(success);
            }
        }


        internal static bool IsPermissionInManifest (Context context, string permission)
        {
            try
            {
                var info = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Permissions);
                return info.RequestedPermissions.Contains(permission);
            }
            catch
            {
            }

            return false;
        }

        internal static bool IsPermissionGranted(Context context, string permission)
        {
            return ContextCompat.CheckSelfPermission(context, permission) == Permission.Granted;
        }

        internal static bool DoRequestPermissions(Activity activity, string[] permissions, int requestCode)
        {
            var permissionsToRequest = new List<string>();
            foreach (var permission in permissions)
            {
                if (ContextCompat.CheckSelfPermission(activity, permission) != Permission.Granted)
                    permissionsToRequest.Add(permission);
            }

            if (permissionsToRequest.Any())
            {
                ActivityCompat.RequestPermissions(activity, permissionsToRequest.ToArray(), requestCode);
                return true;
            }

            return false;
        }

        internal static bool CheckCameraPermissions(Context context, bool throwOnError = true)
        {
            return CheckPermissions(context, global::Android.Manifest.Permission.Camera, throwOnError);
        }

        internal static bool CheckTorchPermissions(Context context, bool throwOnError = true)
        {
            return CheckPermissions(context, global::Android.Manifest.Permission.Flashlight, throwOnError);
        }

        internal static bool CheckPermissions(Context context, string permission, bool throwOnError = true)
        {
            var result = true;

            System.Diagnostics.Debug.WriteLine("Checking " + permission + "...");

            if (!IsPermissionInManifest(context, permission) || !IsPermissionGranted(context, permission))
            {
                result = false;

                if (throwOnError)
                {
                    var msg = "CameraTF requires: " + permission + ", but was not found in your AndroidManifest.xml file.";
                    System.Diagnostics.Debug.WriteLine(msg);

                    throw new UnauthorizedAccessException(msg);
                }
            }

            return result;
        }
    }
}

