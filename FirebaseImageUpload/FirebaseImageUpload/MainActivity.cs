using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using System;
using Android.Content;
using Android.Runtime;
using Android.Graphics;
using Android.Provider;
using Java.IO;
using Firebase.Storage;
using Firebase;
using Android.Gms.Tasks;

namespace FirebaseImageUpload
{
    [Activity(Label = "FirebaseImageUpload", MainLauncher = true, Theme ="@style/Theme.AppCompat.Light.NoActionBar")]
    public class MainActivity : AppCompatActivity,
        IOnProgressListener, IOnSuccessListener, IOnFailureListener
    {
        private Button          btnChoose, 
                                btnUpload,
                                btnDownload;
        private ImageView       imgView,
                                imgView2;
        private Android.Net.Uri filePath;
        private const int       PICK_IMAGE_REQUEST = 71;

        ProgressDialog progressDialog;

        FirebaseStorage storage;
        StorageReference storageRef;

        private int TASK = 0 ,
                    UPLOAD = 1,
                    DOWNLOAD = 2;

        string guid = Guid.NewGuid().ToString();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Firebase Initialization
            FirebaseApp.InitializeApp(this);
            storage = FirebaseStorage.Instance;
            storageRef = storage.GetReferenceFromUrl("gs://ontargetbeta.appspot.com");

            btnChoose = FindViewById<Button>(Resource.Id.btnChoose);
            btnUpload = FindViewById<Button>(Resource.Id.btnUpload);
            btnDownload = FindViewById<Button>(Resource.Id.btnDownload);
            imgView = FindViewById<ImageView>(Resource.Id.imgView);
            imgView2 = FindViewById<ImageView>(Resource.Id.imgView2);

            btnChoose.Click += delegate {
                ChooseImage();
            };

            btnUpload.Click += delegate {
                UploadImage();
            };

            btnDownload.Click += delegate
            {
                DonloadImage();
            };
        }

        private void DonloadImage()
        {
            TASK = DOWNLOAD;
            progressDialog = new ProgressDialog(this);
            progressDialog.SetTitle("Downloading");
            progressDialog.Window.SetType(Android.Views.WindowManagerTypes.SystemAlert);
            progressDialog.Show();

            var images = storageRef.Child("images/" + guid);

            Java.IO.File file = new Java.IO.File(GetExternalFilesDir(null), guid + ".jpg");

            images.GetFile(file)
                    .AddOnProgressListener(this)
                    .AddOnSuccessListener(this)
                    .AddOnFailureListener(this);
        }

        private void UploadImage()
        {
            if (filePath != null)
            {
                TASK = UPLOAD;

                progressDialog = new ProgressDialog(this);
                progressDialog.SetTitle("Uploading");
                progressDialog.Window.SetType(Android.Views.WindowManagerTypes.SystemAlert);
                progressDialog.Show();

                var images = storageRef.Child("images/" + guid);
                images.PutFile(filePath)
                    .AddOnProgressListener(this)
                    .AddOnSuccessListener(this)
                    .AddOnFailureListener(this);
            }
        }

        private void ChooseImage()
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(intent, "Select an image"), PICK_IMAGE_REQUEST);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PICK_IMAGE_REQUEST &&
                resultCode == Result.Ok &&
                data != null &&
                data.Data != null)
            {
                filePath = data.Data;
                try
                {
                    Bitmap bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, filePath);
                    imgView.SetImageBitmap(bitmap);

                    TASK = 0;
                }
                catch (IOException ex)
                {
                    ex.PrintStackTrace();
                }
            }
        }

        public void OnProgress(Java.Lang.Object snapshot)
        {
            if (TASK == UPLOAD)
            {
                var taskSnapShot = (UploadTask.TaskSnapshot)snapshot;
                double progress = (100.0 * taskSnapShot.BytesTransferred / taskSnapShot.TotalByteCount);
                progressDialog.SetMessage("Uploaded " + (int)progress + "%");
            }
            if (TASK == DOWNLOAD)
            {

                var taskSnapShot = (FileDownloadTask.TaskSnapshot)snapshot;
                double progress = (100.0 * taskSnapShot.BytesTransferred / taskSnapShot.TotalByteCount);
                progressDialog.SetMessage("Downloaded " + (int)progress + "%");
            }
            
        }

        public void OnSuccess(Java.Lang.Object result)
        {
            if (TASK == UPLOAD)
            {
                progressDialog.Dismiss();
                Toast.MakeText(this, "Uploaded", ToastLength.Short).Show();
            }
            else // TASK == DOWNLOAD
            {
                progressDialog.Dismiss();
                Toast.MakeText(this, "Downloaded", ToastLength.Short).Show();

                Java.IO.File file = new Java.IO.File(GetExternalFilesDir(null), guid + ".jpg");

                BitmapFactory.Options options = new BitmapFactory.Options();
                options.InPreferredConfig = Bitmap.Config.Argb8888;
                Bitmap bitmap = BitmapFactory.DecodeFile(file.Path, options);

                imgView2.SetImageBitmap(bitmap);
            }
        }

        public void OnFailure(Java.Lang.Exception e)
        {
            progressDialog.Dismiss();
            Toast.MakeText(this, e.Message, ToastLength.Short).Show();
        }
    }
}
