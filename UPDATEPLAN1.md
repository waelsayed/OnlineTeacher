Before proceeding to implementation, make one clarification to the database constraints:

**Slug must NOT be globally unique.**

The canonical Teacher Platform URL is identified by the combination:

`PublicId + Slug`

`PublicId` must be globally unique.

Slug should be treated as an SEO/canonical URL component, not as the primary identity of the Teacher Platform.

Therefore:

* `PublicId` → globally UNIQUE
* `Slug` → do NOT enforce global uniqueness unless the approved architecture explicitly requires it
* Route resolution must always validate both `PublicId` and `Slug`
* Valid PublicId + incorrect/old slug → 301 redirect to the current canonical URL
* Invalid PublicId → 404

Keep the rest of the approved Phase 1 plan unchanged.

After applying this clarification, proceed with the approved implementation sequence starting from Step 0.
