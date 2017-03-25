// Copyright (c) 2006 by M. David Peterson
// The code contained in this file is licensed under a Creative Commons (Attribution 3.0) license
// Please see http://creativecommons.org/licenses/by/3.0/us/ for specific detail.

using System;
using System.Web;
using Nuxleus.Asynchronous;

namespace Nuxleus.Web.HttpHandler {

    public struct NuxleusHttpAsyncProtoBufHandler : IHttpAsyncHandler {

        static object m_lock = new object();

        public void ProcessRequest(HttpContext context) {
            //not called
        }

        public bool IsReusable {
            get { return false; }
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData) {

            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            NuxleusAsyncResult nuxleusAsyncResult = new NuxleusAsyncResult(cb, extraData);

            nuxleusAsyncResult.CompleteCall();
            return nuxleusAsyncResult;
        }

        public void EndProcessRequest(IAsyncResult result) {

        }

    }
}
