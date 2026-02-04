using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrandVue;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd
{
    internal class ExtensionsTests
    {
        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("some text", "Some Text")]
        [TestCase("a very long title, i mean you wouldn't believe me if i told you that you could have a title this long!", "A Very Long Title, I Mean You Wouldn't Believe Me If I Told You That You Could Have A Title This Long!")]
        [TestCase("WHAT ABOUT FROM CAPS", "What About From Caps")]
        [TestCase("one 2 three 4 5ive Six", "One 2 Three 4 5ive Six")]
        [TestCase("we_can_have_underscores_too", "We Can Have Underscores Too")]
        [TestCase("andré the GIANT!", "André The Giant!")]
        [TestCase("x æ a-12", "X Æ A-12")]
        public void TitleCaseTest(string original, string expected)
        {
            Assert.That(original.ToTitleCaseString(), Is.EqualTo(expected));
        }

        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("<div>text</div>", "text")]
        [TestCase("<div>text", "text")]
        [TestCase("<h1>text</h1><p>some more text<p>", "textsome more text")]
        [TestCase(@"<!doctype html>
<html>
  <head>
    <title>This is the title of the webpage!</title>
  </head>
  <body>
    <p>This is an example paragraph. Anything in the <strong>body</strong> tag will appear on the page, just like this <strong>p</strong> tag and its contents.</p>
  </body>
</html>", @"

  
    This is the title of the webpage!
  
  
    This is an example paragraph. Anything in the body tag will appear on the page, just like this p tag and its contents.
  
")]
        public void StripHtmlTagsTest(string original, string expected)
        {
            Assert.That(original.StripHtmlTags(), Is.EqualTo(expected));
        }
    }
}
