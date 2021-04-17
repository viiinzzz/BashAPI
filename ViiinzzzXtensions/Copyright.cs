
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Xtensions.Std
{
    /// <summary>
    /// static class to display author's logo in console.
    /// </summary>
    public static class Copyright
    {
        private static FileVersionInfo assInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

        /// <summary>
        /// Company name
        /// </summary>
        public readonly static string company = assInfo.CompanyName;
        /// <summary>
        /// copyright prepended company name
        /// </summary>
        public readonly static string C_company = $"{assInfo.LegalCopyright} {company}";
        /// <summary>
        /// license issue date
        /// </summary>
        public readonly static string pkgdesc = assInfo.Comments;
        /// <summary>
        /// license issue date
        /// </summary>
        public readonly static string term_date = assInfo.ProductVersion;
        /// <summary>
        /// package major
        /// </summary>
        public readonly static int major = assInfo.ProductMajorPart;
        /// <summary>
        /// package major.minor
        /// </summary>
        public readonly static string major_minor = $"{assInfo.ProductMajorPart}.{assInfo.ProductMinorPart}";

        /// <summary>
        /// splash text with assembly name version and company copyright
        /// </summary>
        public readonly static string splash = @$"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version}
{Assembly.GetExecutingAssembly().GetName().Name.ToAsciiArt()}
{Copyright.C_company}";

        private const string asciiart_az = @"
   ╔═╗╔╗ ╔═╗╔╦╗╔═╗╔═╗╔═╗╦ ╦ ╦  ╦ ╦╔═╦  ╔╦╗╔╗╔╔═╗╔═╗╔═╗╦═╗╔═╗╔╦╗╦ ╦╦ ╦╦ ╦═╗╦╦ ╦╔═╗
   ╠═╣╠╩╗║   ║║║╣ ╠╣ ║ ╦╠═╣ ║  ║ ╠╩╗║  ║║║║║║║ ║╠═╝║ ╣╠╦╝╚═╗ ║ ║ ║╚╗║║║║╔╬╝╚╦╝╔═╝
   ╩ ╩╚═╝╚═╝═╩╝╚═╝╚  ╚═╝╩ ╩ ╩ ╚╝ ╩ ╩╩═╝╩ ╩╝╚╝╚═╝╩  ╚═╬╩╚═╚═╝ ╩ ╚═╝ ╚╝╚╩╝╩╚═ ╩ ╚═╝";

        /// <summary>
        /// convert string to ASCII Art
        /// </summary>
        /// <param name="x">string</param>
        /// <returns></returns>
        public static string ToAsciiArt(this string x)
        {
            var lines = asciiart_az.Split(new char[] { '\n' }).Skip(1);
            return x.ToUpper()
                .Select(c => c >= 'A' && c <= 'Z' ? c - 'A' + 1 : 0)
                .Select(i => lines.Select(line => line.Substring(i * 3, 3)))
                .Aggregate((c1, c2) => c1.Zip(c2, (l1, l2) => l1 + l2))
                .Aggregate((l1, l2) => l1 + "\n" + l2);
        }

    }
}
