ARG IMAGE_PREFIX=""
FROM ${IMAGE_PREFIX}web-artifacts:latest as builder
FROM ${IMAGE_PREFIX}web-runtime:latest

# Copy the build artifacts from the 'builder' image
# to the distribution folder on the runtime image.
COPY --from=builder /opt/app-root/src/dist/. /tmp/app/dist/

RUN whoami

# Ensure S2I script copied over is runnable.
COPY ./s2i/bin/fix-base-url /usr/libexec/s2i/fix-base-url

# Fix permissions.
USER root
RUN chmod 755 /usr/libexec/s2i/fix-base-url

# From nginx-runtime.
USER 104

# Since the runtime image is itself an s2i image we need to
# short circuit the s2i lifecycle.
# The runtime image "loses" its s2i runtime voodoo when it 
# is used in a dockerSrategy, which is why the explicit `CMD` is necessary
CMD  /usr/libexec/s2i/fix-base-url