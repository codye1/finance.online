const categoriesApi = {
    createCategory: function (categoryData) {
        return $.ajax({
            url: '/categories/create',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(categoryData)
        });
    },
    deleteCategory: function (categoryId) {
        return $.ajax({
            url: '/categories/delete',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ categoryId: categoryId })
        });
    }
};

export default categoriesApi;