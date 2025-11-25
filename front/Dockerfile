FROM nginx:alpine

COPY front/nginx.conf /etc/nginx/conf.d/default.conf
COPY front/assets /usr/share/nginx/html/assets
COPY front/index.html /usr/share/nginx/html
COPY front/login.html /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
