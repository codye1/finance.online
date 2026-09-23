const homeApi = {
    createOperation: function (operationData, organizationId) {
        return $.ajax({
            url: '/organizations/' + organizationId + '/operations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(operationData)
        });
    },
    createOrganization: function (organizationData) {
        return $.ajax({
            url: '/organizations',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(organizationData)
        });
    },
    loadMoreOperations: function (organizationId, page, period) {
        return $.ajax({
            url: '/organizations/' + organizationId + '/operations/more',
            method: 'GET',
            data: { page: page, period: period }
        });
    }
};

export default homeApi;