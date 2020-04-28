using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatBuffers;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;
using Service.Localization;
using TMPro;

namespace ___MOD_NAME___.Services
{
    public class LocalizationService : ProxyService<LocalizationService, ILocalizationService>
    {
        private const string ADDITIONAL_TEXT = "\n\\u2022 Check out <u><link=”https://github.com/MyUserName/___MOD_NAME___>___MOD_NAME_FULL___</link></u>";

        [LogicProxy]
        public void Localize(Keys locaKey, TextMeshProUGUI textOutput, Dictionary<string, string> replacements, ReplacementStyle replacementStyle)
        {
            this.Vanilla.Localize(locaKey, textOutput, replacements, replacementStyle);

            if (locaKey == Keys.Common_EarlyAccessDisclaimer)
            {
                var text = textOutput.text;
                text += ADDITIONAL_TEXT;
                textOutput.text = text;
            }
        }

        [LogicProxy]
        public string GetLocalization(Keys locaKey, Dictionary<string, string> replacements, ReplacementStyle replacementStyle)
        {
            var text = this.Vanilla.GetLocalization(locaKey, replacements);
            
            if (locaKey == Keys.Common_EarlyAccessDisclaimer)
            {
                text += ADDITIONAL_TEXT;
            }

            return text;
        }
    }
}
