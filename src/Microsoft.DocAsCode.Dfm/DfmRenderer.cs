// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Immutable;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class DfmRenderer : HtmlRenderer
    {
        private static readonly DocfxFlavoredIncHelper _inlineInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DocfxFlavoredIncHelper _blockInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DfmCodeExtractor _dfmCodeExtractor = new DfmCodeExtractor();

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer result = "<xref";
            if (token.Href != null)
            {
                result += " href=\"";
                result += StringHelper.HtmlEncode(token.Href);
                result += "\"";
            }
            if (token.Title != null)
            {
                result += " title=\"";
                result += StringHelper.HtmlEncode(token.Title);
                result += "\"";
            }
            result += ">";
            if (token.Name != null)
            {
                result += StringHelper.HtmlEncode(token.Name);
            }
            result += "</xref>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            var href = token.Src == null ? null : $"src=\"{StringHelper.HtmlEncode(token.Src)}\"";
            var name = token.Name == null ? null : StringHelper.HtmlEncode(token.Name);
            var title = token.Title == null ? null : $"title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            var resolved = _blockInclusionHelper.Load(renderer, token.Src, token.Raw, context, ((DfmEngine)renderer.Engine).InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            var resolved = _inlineInclusionHelper.Load(renderer, token.Src, token.Raw, context, ((DfmEngine)renderer.Engine).InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            if (string.IsNullOrEmpty(token.Content))
            {
                return StringBuffer.Empty;
            }
            StringBuffer result = "<yamlheader>";
            result += StringHelper.HtmlEncode(token.Content);
            return result + "</yamlheader>";
        }

        public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = string.Empty;
            var splitTokens = DfmRendererHelper.SplitBlockquoteTokens(token.Tokens);
            foreach (var splitToken in splitTokens)
            {
                if (splitToken.Token is DfmSectionBlockToken)
                {
                    content += "<div";
                    content += ((DfmSectionBlockToken)splitToken.Token).Attributes;
                    content += ">";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</div>\n";
                }
                else if (splitToken.Token is DfmNoteBlockToken)
                {
                    var noteToken = (DfmNoteBlockToken)splitToken.Token;
                    content += "<div class=\"";
                    content += noteToken.NoteType;
                    content += "\"><h5>";
                    content += noteToken.NoteType;
                    content += "</h5>";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</div>\n";
                }
                else
                {
                    content += "<blockquote>";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</blockquote>\n";
                }
            }
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            if (!PathUtility.IsRelativePath(token.Path))
            {
                string errorMessage = $"Code absolute path: {token.Path} is not supported in file {context.GetFilePathStack().Peek()}";
                Logger.LogError(errorMessage);
                return DfmRendererHelper.GetRenderedFencesBlockString(token, renderer.Options, errorMessage);
            }

            try
            {
                // TODO: Valid REST and REST-i script.
                var fencesPath = ((RelativePath)token.Path).BasedOn((RelativePath)context.GetFilePathStack().Peek());
                var extractResult = _dfmCodeExtractor.ExtractFencesCode(token, fencesPath);
                return DfmRendererHelper.GetRenderedFencesBlockString(token, renderer.Options, extractResult.ErrorMessage, extractResult.FencesCodeLines);
            }
            catch (FileNotFoundException)
            {
                string errorMessage = $"Can not find reference {token.Path}";
                Logger.LogError(errorMessage);
                return DfmRendererHelper.GetRenderedFencesBlockString(token, renderer.Options, errorMessage);
            }
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return token.Content;
        }
    }
}