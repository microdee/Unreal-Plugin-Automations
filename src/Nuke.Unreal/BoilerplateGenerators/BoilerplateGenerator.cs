using System;
using System.Linq;
using System.Reflection;
using System.IO;
using Nuke.Common.IO;
using Scriban;

namespace Nuke.Unreal.BoilerplateGenerators
{
    public class BoilerplateGenerator
    {
        public static AbsolutePath DefaultTemplateFolder =>
            (AbsolutePath) Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) / "Templates";

        protected static void CheckErrors(Template template)
        {
            if(!template.HasErrors) return;
            throw new ScribanParseException(template);
        }

        protected static void RenderFile(AbsolutePath templateRoot, RelativePath source, AbsolutePath destinationFolder, object model)
        {
            var relFileTemplate = Template.Parse(source, templateRoot / source);
            CheckErrors(relFileTemplate);

            var textTemplate = Template.Parse(File.ReadAllText(templateRoot / source), templateRoot / source);
            CheckErrors(textTemplate);

            var renderedRelFilePath = relFileTemplate.Render(model);
            var renderedText = textTemplate.Render(model);

            var resultPath = destinationFolder / renderedRelFilePath;
            var resultFilename = Path.GetFileNameWithoutExtension(resultPath);
            var resultExt = Path.GetExtension(resultPath).Replace("sbn", "");

            File.WriteAllText((resultPath.Parent / resultFilename) + resultExt, renderedText);
        }

        protected static void RenderFolder(AbsolutePath templateRoot, AbsolutePath destinationFolder, object model, AbsolutePath currentFolder = null)
        {
            currentFolder ??= templateRoot;
            foreach(var file in Directory.EnumerateFiles(currentFolder))
            {
                var relPath = (RelativePath) Path.GetRelativePath(templateRoot, file);
                RenderFile(templateRoot, relPath, destinationFolder, model);
            }

            foreach(var dir in Directory.EnumerateDirectories(currentFolder))
            {
                RenderFolder(templateRoot, destinationFolder, model, (AbsolutePath) dir);
            }
        }
    }

    public class CommonModelBase
    {
        public string Name { get; init; }
        public string Copyright { get; init; }
    }

    public class ScribanParseException : Exception
    {
        public ScribanParseException(Template template) : base(GetMessage(template))
        {
        }

        private static string GetMessage(Template template)
        {
            var nl = Environment.NewLine;
            var errors = string.Join(nl + "    ", template.Messages.Cast<string>());
            return $"Parsing scriban template threw an error:{nl}"
                + $"  at {template.SourceFilePath}:{nl}    {errors}";
        }
    }
}