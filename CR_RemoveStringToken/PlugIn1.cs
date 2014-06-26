using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.CodeRush.Core;
using DevExpress.CodeRush.PlugInCore;
using DevExpress.CodeRush.StructuralParser;

namespace CR_RemoveStringToken
{
    public partial class PlugIn1 : StandardPlugIn
    {
        // DXCore-generated code...
        #region InitializePlugIn
        public override void InitializePlugIn()
        {
            base.InitializePlugIn();
            registerRemoveStringToken();
        }
        #endregion
        #region FinalizePlugIn
        public override void FinalizePlugIn()
        {
            base.FinalizePlugIn();
        }
        #endregion
        public void registerRemoveStringToken()
        {
            DevExpress.CodeRush.Core.CodeProvider RemoveStringToken = new DevExpress.CodeRush.Core.CodeProvider(components);
            ((System.ComponentModel.ISupportInitialize)(RemoveStringToken)).BeginInit();
            RemoveStringToken.ProviderName = "RemoveStringToken"; // Should be Unique
            RemoveStringToken.DisplayName = "Remove String Token";
            RemoveStringToken.CheckAvailability += RemoveStringToken_CheckAvailability;
            RemoveStringToken.Apply += RemoveStringToken_Apply;
            ((System.ComponentModel.ISupportInitialize)(RemoveStringToken)).EndInit();
        }
        private void RemoveStringToken_CheckAvailability(Object sender, CheckContentAvailabilityEventArgs ea)
        {
            // If not in string
            PrimitiveExpression ThePrimitive = (PrimitiveExpression)CodeRush.Source.GetNodeAt(CodeRush.Caret.SourcePoint);
            if (ThePrimitive == null)
                return;
            if (ThePrimitive.PrimitiveType != PrimitiveType.String)
                return;

            string stringCandidate = ThePrimitive.Name;

            // If not on token
            string token = GetTokenNearCaret(ThePrimitive);

            // If string not in call to String.Format
            if (!IsStringToken(token))
                return;

            // If not in method call
            if (!(WithinStringFormat(ThePrimitive) || WithinConsoleWriteLine(ThePrimitive)))
                return;

            ea.Available = true;
        }
        private bool WithinStringFormat(PrimitiveExpression ThePrimitive)
        {
            if (ThePrimitive.Parent.ElementType != LanguageElementType.MethodCallExpression)
                return false;
            MethodCallExpression MCE = (MethodCallExpression)ThePrimitive.Parent;
            MethodReferenceExpression MRE = (MethodReferenceExpression)MCE.Nodes[0];

            if (MRE.Name != "Format" || MRE.Qualifier.Name.ToLower() != "string")
                return false;

            return true;
        }
        private bool WithinConsoleWriteLine(PrimitiveExpression ThePrimitive)
        {
            if (ThePrimitive.Parent.ElementType != LanguageElementType.MethodCall)
                return false;
            MethodCall MC = (MethodCall)ThePrimitive.Parent;
            MethodReferenceExpression MRE = (MethodReferenceExpression)MC.Nodes[0];

            if (MRE.Name.ToLower() != "writeline" || MRE.Qualifier.Name.ToLower() != "console")
                return false;

            return true;
        }
        private string GetTokenNearCaret(PrimitiveExpression activeString)
        {
            var PosInString = CodeRush.Caret.SourcePoint.Offset - activeString.Range.Start.Offset;
            string text = activeString.Name;
            return GetBraceTokenNearCaretExtracted(PosInString, text);
        }
        public static string GetTokenNearCaretExtracted(int PosInString, string text)
        {
            var FirstSpaceBeforeIndex = text.LastIndexOf(' ', PosInString);
            var FirstSpaceAfterIndex = text.IndexOf(' ', PosInString);
            return text.Substring(FirstSpaceBeforeIndex + 1, FirstSpaceAfterIndex - FirstSpaceBeforeIndex - 1);
        }
        public static string GetBraceTokenNearCaretExtracted(int PosInString, string text)
        {
            var FirstBraceBeforeIndex = text.LastIndexOf('{', PosInString);
            var FirstBraceAfterIndex = text.IndexOf('}', PosInString);
            return text.Substring(FirstBraceBeforeIndex, FirstBraceAfterIndex - FirstBraceBeforeIndex + 1);
        }

        private bool IsStringToken(string token)
        {
            if (token.Trim() == string.Empty)
                return false;
            if (!token.StartsWith("{"))
                return false;
            if (!token.EndsWith("}"))
                return false;
            if ((from ch in token.Substring(1, token.Length - 2) where !isNumeric(ch) select ch).Any())
                return false;

            return true;
        }
        private bool isNumeric(char ch)
        {
            return "1234567890".Contains(ch);

        }

        private void RemoveStringToken_Apply(Object sender, ApplyContentEventArgs ea)
        {
            using (ea.TextDocument.NewCompoundAction("Remove string Token"))
            {
                PrimitiveExpression ThePrimitive = (PrimitiveExpression)CodeRush.Source.GetNodeAt(CodeRush.Caret.SourcePoint);
                SourceRange PrimitiveNameRange = ThePrimitive.NameRange;
                string text = ThePrimitive.Name;
                string token = GetTokenNearCaret(ThePrimitive);
                IHasArguments MCE = (IHasArguments)ThePrimitive.Parent;


                int tokenOffset = CodeRush.Caret.SourcePoint.Offset - ThePrimitive.Range.Start.Offset; // text.IndexOf(token);
                int caretOffset = tokenOffset - text.LastIndexOf("{", tokenOffset);
                int tokenLength = token.Length;
                int tokenID = int.Parse(token.Substring(1, tokenLength - 2));

                var HigherTokenNumbers = from number in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                                         where text.IndexOf("{" + number + "}") > -1
                                         && number > tokenID
                                         orderby number ascending
                                         select number;

                // Remove Specific occurance of {2} or whatever
                text = text.Remove(tokenOffset - caretOffset, tokenLength);

                // Detect additional copies of {2}. 
                tokenOffset = text.IndexOf(token);
                // if {2} cannot be found assume it has been eliminated
                if (tokenOffset == -1)
                {
                    // Token is now entirely missing from the text. Need to shuffle higher tokens up

                    // Renumber Remaining Tokens
                    // Renumber {3} -> {Highest} with {2} -> {Highest-1}
                    if (HigherTokenNumbers.Count() > 0)
                        for (int tokenIndex = tokenID; tokenIndex < HigherTokenNumbers.First(); tokenIndex++)
                        {
                            text = text.Replace("{" + (tokenIndex + 1) + "}", "{" + (tokenIndex) + "}");
                        }
                    MCE.Arguments.RemoveAt(tokenID + 1);
                    var MCEAsLang = (LanguageElement)MCE;
                    ea.TextDocument.SetText(MCEAsLang.Range, CodeRush.CodeMod.GenerateCode(MCEAsLang));
                }

                // Write string back over original string.
                ea.TextDocument.SetText(PrimitiveNameRange, text);
            }
        }
    }
}