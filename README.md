# TrafoTech

موقع شركة ترافو تيك: Orchard Core CMS بوضع Headless + واجهة Angular تستهلك GraphQL.

## البنية

```
backend/
  TrafoTech.Cms/      تطبيق Orchard Core (.NET 8) — الـ API والـ admin
  TrafoTech.Setup/    ثيم يحمل وصفة الإعداد (أنواع المحتوى، الأدوار، محتوى مبدئي)
frontend/
  trafotech-web/      تطبيق Angular 21 (standalone, zoneless)
```

`TrafoTech.Setup` معرّف كـ **theme** وليس module — مُحصِّل وصفات الإعداد في Orchard
يفحص الثيمات فقط، فوصفة داخل module لا تظهر في شاشة الإعداد أبداً.

## التشغيل

### 1. الـ CMS

```bash
cd backend/TrafoTech.Cms
dotnet run
```

يعمل على `http://localhost:5000`. عند أول تشغيل تظهر شاشة الإعداد:

- **Recipe:** اختر `TrafoTech Headless`
- **Database:** Sqlite (للتطوير)
- أنشئ حساب المدير

الوصفة تفعّل GraphQL وتعرّف أنواع المحتوى وتمنح `ExecuteGraphQL` للزوار المجهولين.

### 2. الواجهة

```bash
cd frontend/trafotech-web
npm install
npm start
```

تعمل على `http://localhost:4200`.

> المنافذ المسموحة في CORS معرّفة في `backend/TrafoTech.Cms/appsettings.json`
> تحت `TrafoTech:AllowedOrigins`. أضف أي منفذ جديد هناك وأعد تشغيل الـ CMS.

## لوحة الإدارة

`http://localhost:5000/admin`

## أنواع المحتوى

| النوع | المسار (Autoroute) | الحقول المخصصة |
|---|---|---|
| `Page` | `{slug}` | Body (HTML)، MetaDescription، HeroImage |
| `Service` | `services/{slug}` | Summary، Description (HTML)، Icon، Order |
| `BlogPost` | `blog/{slug}` | Subtitle، CoverImage، Categories، PublishedOn، MetaDescription، MarkdownBody |
| `BlogCategories` | — | تصنيف (Taxonomy) بمصطلحات من نوع `Category` |

كل الأنواع تحمل `LocalizationPart` لدعم النسخ العربية/الإنجليزية.

## GraphQL

نقطة النهاية: `http://localhost:5000/api/graphql`
واجهة الاستكشاف: `http://localhost:5000/graphiql` (بعد تسجيل الدخول)

مثال:

```graphql
{
  service { displayText summary order path }
  blogPost(first: 5, orderBy: { createdUtc: DESC }) {
    displayText path subtitle publishedOn
    markdownBody { html }
    categories { termContentItems { displayText } }
  }
}
```

### ملاحظات على السكيما

- الحقول النصية المخصصة **مسطّحة** إلى scalars: `summary` و `subtitle` نصوص مباشرة،
  وليست `{ text }`.
- `orderBy` يدعم حقول الميتاداتا المدمجة فقط (`createdUtc`, `displayText`, …) ولا يدعم
  الحقول المخصصة — لذلك ترتيب الخدمات حسب `order` يتم في العميل داخل `ContentService`.
- حقول Html/Media/Taxonomy تبقى كائنات: `body { html }`، `icon { files { url } }`.

## i18n

`I18nService` يدير اللغة (عربي افتراضياً) ويضبط `lang`/`dir` على عنصر `<html>`.
التنسيقات تستخدم CSS logical properties، فلا حاجة لقواعد مرآة منفصلة لكل اتجاه.

نصوص الواجهة في `src/app/core/i18n.service.ts`. أما محتوى الـ CMS فيُترجم من لوحة
الإدارة عبر إنشاء نسخة مترجمة لكل عنصر (Localization).

## الاختبارات

```bash
cd frontend/trafotech-web
npm test
```

## النشر

- `ng build` ينتج `dist/trafotech-web/browser` — استضفه كملفات ثابتة مع
  fallback إلى `index.html` (توجيه SPA).
- في الإنتاج `environment.production.ts` يستخدم `/api/graphql` على نفس الأصل،
  أي ضع الـ CMS خلف نفس الدومين لتفادي CORS.
