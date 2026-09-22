const login = (email, password) => {
    return $.ajax({
        url: '/auth/login',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ email, password })
    });
}

const register = (email, password) => {
    return $.ajax({
        url: '/auth/register',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ email, password })
    });
}

export { login, register };