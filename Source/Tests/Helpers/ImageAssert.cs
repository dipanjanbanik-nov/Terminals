using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Helpers
{
    public class ImageAssert
    {
        internal const string IMAGE_FILE = @"ControlPanel.png";

        internal static void EqualsToExpectedIcon(string deploymentDirectory, Image favoriteIcon)
        {
            bool areEqual = AreEqual(deploymentDirectory, favoriteIcon);
            Assert.IsTrue(areEqual, "The icon wasnt assigned properly.");
        }

        internal static void DoesntEqualsExpectedIcon(string deploymentDirectory, Image favoriteIcon)
        {
            bool areEqual = AreEqual(deploymentDirectory, favoriteIcon);
            Assert.IsFalse(areEqual, "UpdateIcon cant save favorite.");
        }

        private static bool AreEqual(string deploymentDirectory, Image favoriteIcon)
        {
            string fullIconPath = Path.Combine(deploymentDirectory, IMAGE_FILE);
            using (Image expectedImage = Image.FromFile(fullIconPath))
            {
                return AreEqual(expectedImage, favoriteIcon);
            }
        }

        private static bool AreEqual(Image firstImage, Image secondImage)
        {
            if (firstImage.Size != secondImage.Size)
                return false;

            using (var firstBitmap = new Bitmap(firstImage))
            using (var secondBitmap = new Bitmap(secondImage))
            {
                for (int x = 0; x < firstBitmap.Width; x++)
                {
                    for (int y = 0; y < firstBitmap.Height; y++)
                    {
                        if (firstBitmap.GetPixel(x, y).ToArgb() != secondBitmap.GetPixel(x, y).ToArgb())
                            return false;
                    }
                }

                return true;
            }
        }
    }
}