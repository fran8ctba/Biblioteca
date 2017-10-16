using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Crud.AcessoDados;
using Crud.Models;

namespace Crud.Controllers
{
    public class LivrosController : Controller
    {
        private LivroContexto db = new LivroContexto();

        #region INDEX


        public ActionResult Index()
        {

            return View();
        }

        #endregion

        #region LISTAR

        public JsonResult Listar(Livro livro, int pagina = 1, int registros = 10)
        {
            var livros = db.Livros.Include(l => l.Genero);

            int total = livros.Count();

            #region CondicoesFiltro

            if (!String.IsNullOrWhiteSpace(livro.Titulo))
            {
                livros = livros.Where(l => l.Titulo.Contains(livro.Titulo));
            }

            if (!String.IsNullOrWhiteSpace(livro.Autor))
            {
                livros = livros.Where(l => l.Autor.Contains(livro.Autor));
            }

            if (livro.AnoEdicao != 0)
            {
                livros = livros.Where(l => l.AnoEdicao == livro.AnoEdicao);
            }

            if (livro.Valor != decimal.Zero)
            {
                livros = livros.Where(l => l.Valor == livro.Valor);
            }

            #endregion

            var livrosPaginados = livros.OrderBy(l => l.Titulo).Skip((pagina - 1) * registros).Take(registros);

            return Json(new {
                        rows = livrosPaginados.ToList(),
                        current = pagina,
                        rowCount = registros,
                        total = total 
                        }
            , JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region DETAILS

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Livro livro = db.Livros.Find(id);
            if (livro == null)
            {
                return HttpNotFound();
            }
            return View(livro);
        }

        #endregion

        #region CREATE

        public ActionResult Create()
        {
            ViewBag.GeneroId = new SelectList(db.Generos, "Id", "Nome");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Título,Autor,Ano da Edição,Valor,Genero")] Livro livro)
        {
            if (ModelState.IsValid)
            {
                db.Livros.Add(livro);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.GeneroId = new SelectList(db.Generos, "Id", "Nome", livro.GeneroId);
            return View(livro);
        }

        #endregion

        #region EDIT


        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Livro livro = db.Livros.Find(id);
            if (livro == null)
            {
                return HttpNotFound();
            }
            ViewBag.GeneroId = new SelectList(db.Generos, "Id", "Nome", livro.GeneroId);
            return View(livro);
        }

        // POST: Livros/Edit/5
        // Para se proteger de mais ataques, ative as propriedades específicas a que você quer se conectar. Para 
        // obter mais detalhes, consulte https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Titulo,Autor,AnoEdicao,Valor,GeneroId")] Livro livro)
        {
            if (ModelState.IsValid)
            {
                db.Entry(livro).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.GeneroId = new SelectList(db.Generos, "Id", "Nome", livro.GeneroId);
            return View(livro);
        }

        #endregion

        #region DELETE

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Livro livro = db.Livros.Find(id);
            if (livro == null)
            {
                return HttpNotFound();
            }
            return View(livro);
        }

        // POST: Livros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Livro livro = db.Livros.Find(id);
            db.Livros.Remove(livro);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        #endregion

        #region DISPOSE

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
