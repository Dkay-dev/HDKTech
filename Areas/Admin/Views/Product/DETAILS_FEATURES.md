# Product Details Form - Features & Documentation

## 🎯 Overview
Complete, professional product create/edit form with modern UI/UX design. Uses inline CSS for styling consistency with the HDKTech admin interface.

---

## ✨ Features

### 1. **Smart Header**
- Dynamic title (Create/Edit mode)
- Breadcrumb indication
- Quick back navigation button
- Gradient background matching theme

### 2. **Form Sections**

#### Section 1: Basic Information
- **Product Name** - Required field with validation
- **Category** - Dropdown populated from database
- **Brand** - Dropdown populated from database
- **Price** - Currency input with ₫ symbol
- **Suggested Price** - Optional MSRP field
- **Status** - Active/Inactive toggle

#### Section 2: Description & Details
- **Product Description** - Rich text with HTML support
- **Technical Specs** - Pipe-separated specs display
- **Warranty Info** - Warranty details
- **Promotions** - Special offers and gifts

### 3. **Input Styling**
- Professional border styling (#dddddd)
- Red focus states (#ff2222)
- Rounded corners (8px)
- Smooth transitions
- Help text for guidance
- Validation error messages

### 4. **Form Validation**
- Client-side JavaScript validation
- Required field checks
- Number input validation
- Helpful error messages
- Server-side validation support

### 5. **Action Buttons**
- **Cancel** - Return to list (secondary style)
- **Delete** - Remove product with confirmation (if editing)
- **Submit** - Create/Update product (primary red style)
- Responsive button layout

### 6. **Alert Messages**
- Success alerts (green border)
- Error alerts (red border)
- Icon support
- Dismissible design

### 7. **Responsive Design**
- Grid-based 2-column layout
- Single column on mobile
- Flexible inputs
- Proper spacing

---

## 🎨 Color Scheme

```
Primary Red:      #ff2222
Dark Text:        #1a1a1a
Light BG:         #ffffff, #f5f5f5
Border:           #dddddd
Text Secondary:   #666666, #999999
Success:          #28a745
```

---

## 📝 Form Fields Mapping

| Field | Database | Type | Required | Notes |
|-------|----------|------|----------|-------|
| TenSanPham | Product Name | Text | ✓ | Max 100 chars |
| MaDanhMuc | Category ID | Select | ✓ | Foreign key |
| MaHangSX | Brand ID | Select | ✓ | Foreign key |
| Gia | Price | Number | ✓ | Step: 1000 |
| GiaNiemYet | MSRP | Number | ✗ | Optional |
| TrangThaiSanPham | Status | Select | ✓ | 0=Inactive, 1=Active |
| MoTaSanPham | Description | TextArea | ✓ | HTML support |
| ThongSoKyThuat | Tech Specs | TextArea | ✗ | Pipe-separated |
| ThongTinBaoHanh | Warranty | Text | ✗ | Optional |
| KhuyenMai | Promotions | TextArea | ✗ | Pipe-separated |

---

## 🔧 JavaScript Functions

### Form Validation
```javascript
// Validates on submit:
- Product name (required, non-empty)
- Category selection (required)
- Brand selection (required)
- Price (required, > 0)
- Description (required, non-empty)
```

### Number Formatting
- Currency inputs accept only numbers
- Step increment by 1000
- Display with ₫ symbol

---

## 💾 Form Actions

### Create Mode
- POST `/admin/product/create`
- Creates new product
- Redirects to index on success
- Shows success message

### Edit Mode
- POST `/admin/product/edit/{id}`
- Updates existing product
- Include hidden `MaSanPham` field
- Delete button available

### Delete Mode
- POST `/admin/product/delete/{id}`
- Confirmation dialog
- Soft delete or remove from DB
- Redirect to index

---

## 🚀 Usage Examples

### Creating New Product
```
1. Navigate to /admin/product/create
2. Fill in all required fields (*)
3. Add description and specs
4. Click "Tạo sản phẩm" button
5. Success message appears
6. Redirect to product list
```

### Editing Existing Product
```
1. Click edit icon on product row
2. URL: /admin/product/edit/{id}
3. Update any fields
4. Delete button appears (optional)
5. Click "Cập nhật sản phẩm"
6. Success confirmation
```

---

## 🎯 Best Practices

### Data Entry
- Always fill required fields (marked with *)
- Use realistic product names
- Choose correct category and brand
- Set accurate pricing
- Write clear descriptions
- Technical specs separated by | (pipe)

### Content Guidelines
- Description: 50-500 characters
- Warranty: 10-50 characters
- Specs: Use standard format (CPU | GPU | RAM | SSD)
- Promotions: Clear, attractive offers

### SEO Tips
- Use descriptive product names
- Include key specifications in description
- Add warranty details for trust
- List promotions for visibility

---

## 🔐 Security

- Server-side validation required (in Controller)
- HTML content filtering needed (prevent XSS)
- User authorization checks
- CSRF token support
- File upload validation (if added)

---

## 📱 Responsive Breakpoints

- **Desktop** (>768px): 2-column grid layout
- **Tablet** (768px): 1-column layout
- **Mobile** (<768px): Full-width inputs

---

## ✅ Validation Rules

### Client-Side
1. Non-empty required fields
2. Positive number for price
3. Text length checks
4. Email format (if applicable)

### Server-Side (Controller)
1. ModelState validation
2. Database constraints
3. Business logic checks
4. Permission validation

---

## 🎯 Future Enhancements

- [ ] Image upload with preview
- [ ] Multiple images support
- [ ] Rich text editor (TinyMCE)
- [ ] Category/Brand search
- [ ] Price comparison tools
- [ ] Product templates
- [ ] Bulk import/export
- [ ] Version history
- [ ] Product variants
- [ ] Stock management integration

---

## 📞 Support

For issues or questions:
1. Check validation messages
2. Verify all required fields
3. Check server logs for errors
4. Contact development team

---

**Last Updated:** 2024
**Version:** 1.0
**Status:** Production Ready ✅
