using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Codenames.Bot.Commands
{
    public class SendMessageCommand : ICommand
    {
        public string Name => "/send";

        public Task HandleAsync(Message message, GameUpdateHandler handler)
        {
            var text = message.Text ?? message.Caption;
            var entitites = message.Entities ?? message.CaptionEntities;

            var builder = new StringBuilder(text);
            if (entitites != null)
            {
                var entities = entitites.Skip(1).ToList().GroupBy(n => n.Offset).Select(g => g.Last()).ToList(); //Удаляем дубликаты офсетов
                foreach (var res in entities.OrderByDescending(x => x.Offset)) //Сортируем по убыванию, чтобы не сбивались офсеты при форматирование
                {
                    builder = Formatter(builder, res);
                }
            }

            StringBuilder Formatter(StringBuilder message, MessageEntity messageEntity)
            {
                switch (messageEntity.Type)
                {
                    case MessageEntityType.Bold:
                        return FormatterToHtml(message, messageEntity.Offset, messageEntity.Length, "<b>", "</b>"); // Жирный текст
                    case MessageEntityType.Italic:
                        return FormatterToHtml(message, messageEntity.Offset, messageEntity.Length, "<i>", "</i>"); //Курсив
                    case MessageEntityType.Strikethrough:
                        return FormatterToHtml(message, messageEntity.Offset, messageEntity.Length, "<s>", "</s>"); //Добавляет зачеркивание текста
                    case MessageEntityType.Underline:
                        return FormatterToHtml(message, messageEntity.Offset, messageEntity.Length, "<u>", "</u>");  //Выделяет текст подчеркиванием
                    case MessageEntityType.Code:
                        //Выделяет текст моноширинным шрифтом. Используется для выделения большого фрагмента кода с сохранением переносов и пробелов
                        return FormatterToHtml(message, messageEntity.Offset, messageEntity.Length, "<pre>", "</pre>");
                    case MessageEntityType.Spoiler:
                        return FormatterToHtml(message, messageEntity.Offset, messageEntity.Length, "<tg-spoiler>", "</tg-spoiler>"); // Скрытый текст
                }
                return message;
            }

            StringBuilder FormatterToHtml(StringBuilder message, int offset, int length, string symbol, string symbolEnd)
            {
                string temp = message.ToString().Substring(offset, length);
                message = message.Remove(offset, length).Insert(offset, symbol + temp + symbolEnd);
                return message;
            }

            var firstNewLineIndex = text.IndexOf('\n');
            var result = builder.ToString()[(firstNewLineIndex + 1)..];

            return handler.SendAllAsync(result, photo: message.Photo == null ? null : InputFile.FromFileId(message.Photo?.MaxBy(x => x?.FileSize ?? 0).FileId));
        }
    }
}
