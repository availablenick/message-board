FROM mcr.microsoft.com/dotnet/sdk:6.0

ARG USER_ID=1000
ARG GROUP_ID=1000

WORKDIR /app

RUN groupadd -g ${GROUP_ID} app && \
    useradd -g ${GROUP_ID} -u ${USER_ID} -m app && \
    chown ${USER_ID}:${GROUP_ID} /app

USER app:app

COPY --chown=app:app . .
RUN dotnet build

EXPOSE 5000

CMD ["dotnet", "run", "--project=./MessageBoard"]
