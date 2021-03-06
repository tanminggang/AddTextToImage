﻿using AddTextToImage.Data.Service;
using AddTextToImage.Domain.Entities;
using AddTextToImage.ImageGenerator;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

namespace AddTextToImage.WebUI.Controllers
{

    public class ImageController : ApiController
    {
        private readonly IRepository<Model> _modelRepository;
        private readonly IRepository<TextTemplate> _textTemplateRepository;
        private readonly IRepository<ClipartTemplate> _clipartTemplateRepository;

        ///<summary>
        /// Creates a new instance of the ImageController class.
        ///</summary>
        public ImageController(IRepository<Model> modelRepository, IRepository<TextTemplate> textTemplateRepository, IRepository<ClipartTemplate> clipartTemplateRepository)
        {
            this._modelRepository = modelRepository;
            this._textTemplateRepository = textTemplateRepository;
            this._clipartTemplateRepository = clipartTemplateRepository;
        }

        ///<summary>
        ///Returns generated image for TextTemplate or ClipartTemplate item depends on modelItem.ItemType.
        ///</summary>
        [HttpGet]
        public HttpResponseMessage ModelItem(int id, [FromUri]ModelItem modelItem)
        {
            TemplateBase template = null;

            if (!ModelState.IsValid)
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            if (modelItem.ItemType == 0)
            {
                template = _textTemplateRepository.GetAllWithInclude("Font").Where(p => p.Id == modelItem.TemplateId).FirstOrDefault();
            }
            else
            {
                template = _clipartTemplateRepository.GetAllWithInclude("Font").Where(p => p.Id == modelItem.TemplateId).FirstOrDefault();
            }

            if (template == null)
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            string fontPath = HostingEnvironment.MapPath("~/fonts/");

            OutlineTextProcessor outlineTextProcessor = new OutlineTextProcessor(modelItem, template, fontPath);
            Bitmap image = outlineTextProcessor.GetImage();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, ImageFormat.Png);

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);

                response.Content = new ByteArrayContent(memoryStream.ToArray());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return response;
            }
        }

        ///<summary>
        ///Returns a result image. 
        ///</summary>
        [HttpGet]
        public HttpResponseMessage Result(int id)
        {
            HttpResponseMessage response;

            if (!ModelState.IsValid)
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            var model = _modelRepository.Get(id);

            if (model == null)
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            ImageConverter ic = new ImageConverter();
            Image img = (Image)ic.ConvertFrom(model.Image);
            Bitmap bmpResult = new Bitmap(img);

            //FromImage method creates a new Graphics from the specified Image.
            Graphics graphics = Graphics.FromImage(bmpResult);

            foreach (var modelItem in model.Items)
            {
                TemplateBase template = null;

                if (modelItem.ItemType == 0)
                {
                    template = _textTemplateRepository.GetAllWithInclude("Font").Where(p => p.Id == modelItem.TemplateId).FirstOrDefault();
                }
                else
                {
                    template = _clipartTemplateRepository.GetAllWithInclude("Font").Where(p => p.Id == modelItem.TemplateId).FirstOrDefault();
                }

                if (template != null)
                {
                    string fontPath = HostingEnvironment.MapPath("~/fonts/");

                    OutlineTextProcessor outlineTextProcessor = new OutlineTextProcessor(modelItem, template, fontPath);
                    Bitmap image = outlineTextProcessor.GetImage();

                    graphics.DrawImage((Image)image, new Point(modelItem.PositionLeft, modelItem.PositionTop));
                }
            }

            response = new HttpResponseMessage(HttpStatusCode.OK);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                bmpResult.Save(memoryStream, ImageFormat.Png);

                response.Content = new ByteArrayContent(memoryStream.ToArray());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
                response.Content.Headers.ContentDisposition.FileName = "result.png";
            }

            graphics.Flush();
            graphics.Dispose();

            return response;
        }
    }
}