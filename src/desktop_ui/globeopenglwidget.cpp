#include "globeopenglwidget.h"
#include <GL/gl.h>
#include <GL/glu.h>

globeopenglwidget::globeopenglwidget(QWidget *parent)
    : QOpenGLWidget(parent) {
    timer = new QTimer(this);
    connect(timer, &QTimer::timeout, this, [this]() {
        rotation += 0.5f;
        update();
    });
    timer->start(16); // ~60 FPS
}

globeopenglwidget::~globeopenglwidget() {}

void globeopenglwidget::initializeGL() {
    initializeOpenGLFunctions();
    glClearColor(0.1f, 0.1f, 0.15f, 1.0f);
    glEnable(GL_DEPTH_TEST);
}

void globeopenglwidget::resizeGL(int w, int h) {
    glViewport(0, 0, w, h);
}

void globeopenglwidget::paintGL() {
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    gluPerspective(45.0, float(width()) / float(height()), 1.0, 100.0);

    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
    glTranslatef(0.0f, 0.0f, -5.0f);
    glRotatef(rotation, 0.0f, 1.0f, 0.0f);

    GLUquadric *quad = gluNewQuadric();
    gluQuadricDrawStyle(quad, GLU_FILL);
    gluQuadricNormals(quad, GLU_SMOOTH);
    gluSphere(quad, 1.0, 40, 40);
    gluDeleteQuadric(quad);
}
